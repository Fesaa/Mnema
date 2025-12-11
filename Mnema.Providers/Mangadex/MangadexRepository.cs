using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mnema.API.Providers;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Mangadex;

public class MangadexRepository(ILogger<MangadexRepository> logger, IDistributedCache cache, IHttpClientFactory httpClientFactory): IRepository
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new ()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly DistributedCacheEntryOptions _cacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };

    public async Task<PagedList<SearchResult>> SearchPublications(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = "/manga".SetQueryParam("title", request.Query)
            .AddRange("status", request.Modifiers.GetStrings("status"))
            .AddRange("contentRating", request.Modifiers.GetStrings("contentRating"))
            .AddRange("publicationDemographic", request.Modifiers.GetStrings("publicationDemographic"))
            .AddPagination(pagination)
            .AddIncludes();
        
        var result = await GetCachedAsync<SearchResponse>(url.ToString(), cancellationToken);
        if (result.IsErr)
        {
            throw new MnemaException("Failed to search for series", result.Error);
        }

        var response = result.Unwrap();
        if (response.Data == null)
        {
            logger.LogError("Response contained null data, did something go wrong?");
            return PagedList<SearchResult>.Empty();
        }
        
        logger.LogDebug("Found {Amount} items out of {Total} for query {Query}", response.Data.Count, response.Total, request.Query);

        var items = response.Data.Select(searchResult => new SearchResult
        {
            Id = searchResult.Id,
            Name = searchResult.Attributes.LangTitle("en"),
            Provider = Provider.Mangadex,
            Description = searchResult.Attributes.Description.GetValueOrDefault("en"),
            Size = searchResult.Attributes.Size(),
            Tags = [],
            Url = searchResult.RefUrl,
            ImageUrl = searchResult.CoverUrl(),
        });

        return new PagedList<SearchResult>(items, response.Total, response.Offset, response.Limit);
    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var id = request.Id;
        var url = $"/manga/{id}".AddIncludes();

        var result = await GetCachedAsync<MangaResponse>(url, cancellationToken);
        if (result.IsErr)
        {
            logger.LogError(result.Error, "Failed to retrieve information for manga {Id} - {Url}", id, url);
            throw new MnemaException($"Failed to retrieve information for manga {id}", result.Error);
        }

        var language = request.GetStringOrDefault(RequestConstants.LanguageKey, "en");

        var manga = result.Unwrap().Data;
        var chapters = await GetChaptersForSeries(id, language, cancellationToken);

        var tags = manga.Attributes.Tags
            .Where(t => t.Attributes.Name.ContainsKey(language))
            .Select(t => new Tag
            {
                Id = t.Id,
                Value = t.Attributes.Name[language],
                IsMarkedAsGenre = t.Attributes.Group == "genre",
            })
            .ToList();

        var filteredChapters = FilterChapters(chapters.Data, language, request).Select(chapter => new Chapter
        {
            Id = chapter.Id,
            Title = chapter.Attributes.Title,
            VolumeCount = chapter.Attributes.Volume,
            ChapterCount = chapter.Attributes.Chapter,
            ReleaseDate = chapter.Attributes.PublishedAt,
            Tags = [],
            People = [],
            TranslationGroups = chapter.RelationShips
                .Where(r => r.Type is "scanlation_group" or "user")
                .Select(r => r.Id)
                .ToList(),
        }).ToList();
        
        return new Series
        {
            Id = id,
            RefUrl = manga.RefUrl,
            Title = manga.Attributes.LangTitle(language),
            Summary = manga.Attributes.Description.GetValueOrDefault(language, string.Empty),
            Status = manga.Attributes.Status.AsPublicationStatus(),
            AgeRating = manga.Attributes.ContentRating.AsAgeRating(),
            Year = manga.Attributes.Year,
            HighestChapterNumber = manga.Attributes.HighestChapter,
            HighestVolumeNumber = manga.Attributes.HighestVolume,
            Tags = tags,
            People = manga.People,
            Chapters = filteredChapters,
        };
    }

    private async Task<ChaptersResponse> GetChaptersForSeries(string id, string language, CancellationToken cancellationToken, int offSet = 0)
    {
        var url = $"/manga/{id}/feed?order[volume]=desc&order[chapter]=desc"
            .AppendQueryParam("translatedLanguage[]", language)
            .AddPagination(20, offSet)
            .AddAllContentRatings();

        var result = await GetCachedAsync<ChaptersResponse>(url, cancellationToken);
        if (result.IsErr)
        {
            logger.LogError(result.Error, "Failed to retrieve chapter information for manga {Id} with offset {OffSet} - {Url}", id, offSet, url);
            throw new MnemaException($"Failed to retrieve chapter information for manga {id} with offset {offSet}", result.Error);
        }

        var resp = result.Unwrap();

        if (resp.Total < resp.Limit + resp.Offset)
        {
            return resp;
        }

        var extra = await GetChaptersForSeries(id, language, cancellationToken, resp.Limit + resp.Offset);

        resp.Data.AddRange(extra.Data);

        return resp;
    }

    private static List<ChapterData> FilterChapters(IList<ChapterData> chapters, string language, DownloadRequestDto request)
    {
        var scanlationGroup = request.GetStringOrDefault(RequestConstants.ScanlationGroupKey, string.Empty);
        var allowNonMatching = request.GetBool(RequestConstants.AllowNonMatchingScanlationGroupKey, true);
        var downloadOneShots = request.GetBool(RequestConstants.DownloadOneShotKey);

        return chapters
            .GroupBy(c => string.IsNullOrEmpty(c.Attributes.Chapter)
                ? string.Empty
                : $"{c.Attributes.Chapter} - {c.Attributes.Volume}")
            .WhereIf(!downloadOneShots, g => !string.IsNullOrEmpty(g.Key))
            .SelectMany(g =>
            {
                if (string.IsNullOrEmpty(g.Key)) return g.ToList();

                var chapter = g.FirstOrDefault(ChapterFinder(language, scanlationGroup));

                if (chapter == null && allowNonMatching)
                {
                    chapter = g.FirstOrDefault(ChapterFinder(language, string.Empty));
                }

                if (chapter == null) return [];

                return [chapter];
            })
            .ToList();
    }

    private static Func<ChapterData, bool> ChapterFinder(string language, string scanlationGroup)
    {
        return chapter =>
        {
            if (chapter.Attributes.TranslatedLanguage != language) return false;

            // Skip over official publisher chapters, we cannot download these from mangadex
            if (!string.IsNullOrEmpty(chapter.Attributes.ExternalUrl )) return false;

            if (string.IsNullOrEmpty(scanlationGroup)) return true;

            return chapter.RelationShips.FirstOrDefault(r =>
            {
                if (r.Type != "scanlation_group" && r.Type != "user") return false;

                return r.Id == scanlationGroup;
            }) != null;
        };
    }
    
    public Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<Result<TResult, HttpRequestException>> GetCachedAsync<TResult>(string url, CancellationToken cancellationToken = default)
    {
        var cachedResponse = await cache.GetAsJsonAsync<TResult>(url, cancellationToken);
        if (cachedResponse != null)
        {
            return Result<TResult, HttpRequestException>.Ok(cachedResponse);
        }
        
        var client = httpClientFactory.CreateClient(nameof(Provider.Mangadex));

        var result = await client.GetAsync<TResult>(url, JsonSerializerOptions, cancellationToken);
        if (result.IsErr)
        {
            return result;
        }

        var resultValue = result.Unwrap();
        if (resultValue != null)
        {
            await cache.SetAsJsonAsync(url, result.Unwrap(), _cacheEntryOptions, cancellationToken);
        }

        return result;
    }

}