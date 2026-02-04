using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Mangadex;

internal class MangadexRepository : IRepository
{
    public static readonly ConcurrentDictionary<string, string> LinkFormats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["al"] = "https://anilist.co/manga/{0}",
            ["ap"] = "https://www.anime-planet.com/manga/{0}",
            ["bw"] = "https://bookwalker.jp/{0}",
            ["mu"] = "https://www.mangaupdates.com/series.html?id={0}",
            ["nu"] = "https://www.novelupdates.com/series/{0}",
            ["kt"] = "https://kitsu.io/api/edge/manga/{0}",
            ["mal"] = "https://myanimelist.net/manga/{0}",

            ["amz"] = "{0}",
            ["ebj"] = "{0}",
            ["cdj"] = "{0}",
            ["raw"] = "{0}",
            ["engtl"] = "{0}"
        };

    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MangadexRepository> _logger;


    private readonly AsyncLazy<List<FormControlOption>> _tagOptions;

    public MangadexRepository(ILogger<MangadexRepository> logger, IDistributedCache cache,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _tagOptions = new AsyncLazy<List<FormControlOption>>(LoadTagOptions);
    }

    private HttpClient Client => _httpClientFactory.CreateClient(nameof(Provider.Mangadex));

    public async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        var url = "/manga".SetQueryParam("title", request.Query)
            .AddRange("status", request.Modifiers.GetStrings("status"))
            .AddRange("contentRating", request.Modifiers.GetStrings("contentRating"))
            .AddRange("publicationDemographic", request.Modifiers.GetStrings("publicationDemographic"))
            .AddRange("includedTags", request.Modifiers.GetStrings("includeTags"))
            .SetQueryParam("includedTagsMode", request.Modifiers.GetStringOrDefault("includedTagsMode", "AND"))
            .AddRange("excludedTags", request.Modifiers.GetStrings("excludeTags"))
            .SetQueryParam("excludedTagsMode", request.Modifiers.GetStringOrDefault("excludedTagsMode", "OR"))
            .AddOffsetPagination(pagination)
            .AddIncludes();

        var result =
            await Client.GetCachedAsync<SearchResponse>(url.ToString(), _cache, cancellationToken: cancellationToken);
        if (result.IsErr) throw new MnemaException("Failed to search for series", result.Error);

        var response = result.Unwrap();
        if (response.Data == null) return PagedList<SearchResult>.Empty();

        var items = response.Data.Select(searchResult => new SearchResult
        {
            Id = searchResult.Id,
            Name = searchResult.Attributes.LangTitle("en"),
            Provider = Provider.Mangadex,
            Description = searchResult.Attributes.Description.GetValueOrDefault("en"),
            Size = searchResult.Attributes.Size(),
            Tags = [],
            Url = searchResult.RefUrl,
            ImageUrl = searchResult.CoverUrl() ?? string.Empty
        });

        return new PagedList<SearchResult>(items, response.Total, response.Offset / response.Limit, response.Limit);
    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var id = request.Id;
        var url = $"/manga/{id}".AddIncludes();

        var result =
            await Client.GetCachedAsync<MangaResponse>(url.ToString(), _cache, cancellationToken: cancellationToken);
        if (result.IsErr) throw new MnemaException($"Failed to retrieve information for manga {id}", result.Error);

        var language = request.GetStringOrDefault(RequestConstants.LanguageKey, "en");

        var manga = result.Unwrap().Data;
        var chapters = await GetChaptersForSeries(id, language, cancellationToken);

        var tags = manga.Attributes.Tags
            .Where(t => t.Attributes.Name.ContainsKey(language))
            .Select(t => new Tag
            {
                Id = t.Id,
                Value = t.Attributes.Name[language],
                IsMarkedAsGenre = t.Attributes.Group == "genre"
            })
            .ToList();

        var filteredChapters = FilterChapters(chapters.Data, language, request).Select((chapter, idx) => new Chapter
        {
            Id = chapter.Id,
            Title = chapter.Attributes.Title ?? string.Empty,
            VolumeMarker = chapter.Attributes.Volume ?? string.Empty,
            ChapterMarker = chapter.Attributes.Chapter ?? string.Empty,
            SortOrder = idx,
            ReleaseDate = chapter.Attributes.PublishAt,
            Tags = [],
            People = [],
            TranslationGroups = chapter.RelationShips
                .Where(r => r.Type is "scanlation_group" or "user")
                .Select(r => r.Id)
                .ToList()
        }).ToList();

        return new Series
        {
            Id = id,
            RefUrl = manga.RefUrl,
            CoverUrl = manga.CoverUrl(),
            NonProxiedCoverUrl = manga.CoverUrl(false),
            Title = manga.Attributes.LangTitle(language),
            Summary = manga.Attributes.Description.GetValueOrDefault(language, string.Empty),
            Status = manga.Attributes.Status.AsPublicationStatus(),
            AgeRating = manga.Attributes.ContentRating.AsAgeRating(),
            Year = manga.Attributes.Year,
            HighestChapterNumber = manga.Attributes.HighestChapter,
            HighestVolumeNumber = manga.Attributes.HighestVolume,
            Links = manga.Attributes.Links
                .Select(kv =>
                    LinkFormats.TryGetValue(kv.Key, out var format) ? string.Format(format, kv.Value) : string.Empty)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList(),
            Tags = tags,
            People = manga.People,
            Chapters = filteredChapters
        };
    }

    public async Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        var url = $"/at-home/server/{chapter.Id}";

        var result =
            await Client.GetCachedAsync<ChapterImagesResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr) throw new MnemaException("Failed to retrieve chapter images", result.Error);

        var imageInfo = result.Unwrap();
        var baseUrl = imageInfo.BaseUrl;
        var hash = imageInfo.Chapter.Hash;

        return imageInfo.Chapter.Data.Select(image =>
        {
            var preferredUrl = $"{baseUrl}/data/{hash}/{image}";
            // Mangadex is timing out on single chapter images. For these we'll get them from the fallback
            var fallbackUrl = $"https://uploads.mangadex.org/data/{hash}/{image}";

            return new DownloadUrl(preferredUrl, fallbackUrl);
        }).ToList();
    }

    public async Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        var url = "chapter"
            .SetQueryParam("limit", 50)
            .SetQueryParam("offset", 0)
            .SetQueryParam("includes[]", "manga")
            .SetQueryParam("translatedLanguage[]", "en")
            .AddAllContentRatings()
            .SetQueryParam("order[readableAt]", "desc");

        var result = await Client.GetCachedAsync<ChaptersResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException("Failed to load recently updated chapters", result.Error);

        return result.Unwrap()
            .Data
            .Select(chapter =>
            {
                var relationShip = chapter.RelationShips.FirstOrDefault(r => r.Type == "manga");
                if (relationShip == null) return null;

                var json = JsonSerializer.Serialize(relationShip.Attributes);
                var mangaAttr = JsonSerializer.Deserialize<MangaAttributes>(json, HttpClientExtensions.JsonSerializerOptions);

                return new ContentRelease
                {
                    ReleaseId = chapter.Id,
                    ReleaseName = chapter.Attributes.Title ?? string.Empty,
                    ContentId = relationShip.Id,
                    ContentName = mangaAttr?.LangTitle("en") ?? string.Empty,
                    ReleaseDate = chapter.Attributes.PublishAt.ToUniversalTime(),
                    Provider = Provider.Mangadex,
                };
            })
            .WhereNotNull()
            .ToList();
    }

    public Task<List<FormControlDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = RequestConstants.LanguageKey,
                Type = FormType.DropDown,
                DefaultOption = "en",
                Options =
                [
                    new FormControlOption("en"),
                    new FormControlOption("zh"),
                    new FormControlOption("zh-hk"),
                    new FormControlOption("es"),
                    new FormControlOption("fr"),
                    new FormControlOption("ja")
                ]
            },
            new FormControlDefinition
            {
                Key = RequestConstants.ScanlationGroupKey,
                Advanced = true,
                Type = FormType.Text
            },
            new FormControlDefinition
            {
                Key = RequestConstants.DownloadOneShotKey,
                Type = FormType.Switch
            },
            new FormControlDefinition
            {
                Key = RequestConstants.IncludeCover,
                Type = FormType.Switch,
                DefaultOption = "true"
            },
            new FormControlDefinition
            {
                Key = RequestConstants.UpdateCover,
                Advanced = true,
                Type = FormType.Switch
            },
            new FormControlDefinition
            {
                Key = RequestConstants.TitleOverride,
                Advanced = true,
                Type = FormType.Text
            },
            new FormControlDefinition
            {
                Key = RequestConstants.AllowNonMatchingScanlationGroupKey,
                Advanced = true,
                Type = FormType.Switch,
                DefaultOption = "true"
            }
        ]);
    }

    public async Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return
        [
            new FormControlDefinition
            {
                Type = FormType.MultiSelect,
                Key = "status",
                Options =
                [
                    FormControlOption.Option("Cancelled", "cancelled"),
                    FormControlOption.Option("Completed", "completed"),
                    FormControlOption.Option("Hiatus", "hiatus"),
                    FormControlOption.Option("Ongoing", "ongoing")
                ]
            },
            new FormControlDefinition
            {
                Type = FormType.MultiSelect,
                Key = "contentRating",
                Options =
                [
                    FormControlOption.Option("Safe", "safe"),
                    FormControlOption.Option("Suggestive", "suggestive"),
                    FormControlOption.Option("Erotica", "erotica"),
                    FormControlOption.Option("Pornographic", "mature")
                ]
            },
            new FormControlDefinition
            {
                Type = FormType.MultiSelect,
                Key = "includeTags",
                Options = await _tagOptions
            },
            new FormControlDefinition
            {
                Type = FormType.MultiSelect,
                Key = "excludeTags",
                Options = await _tagOptions
            },
            new FormControlDefinition
            {
                Type = FormType.DropDown,
                Key = "includeTagsMode",
                Options = [FormControlOption.DefaultValue("And", "AND"), FormControlOption.Option("Or", "OR")]
            },
            new FormControlDefinition
            {
                Type = FormType.DropDown,
                Key = "excludeTagsMode",
                Options = [FormControlOption.Option("And", "AND"), FormControlOption.DefaultValue("Or", "OR")]
            }
        ];
    }

    private async Task<ChaptersResponse> GetChaptersForSeries(string id, string language,
        CancellationToken cancellationToken, int offSet = 0)
    {
        var url = $"/manga/{id}/feed?order[volume]=desc&order[chapter]=desc"
            .AppendQueryParam("translatedLanguage[]", language)
            .AddOffsetPagination(20, offSet)
            .AddAllContentRatings();

        var result = await Client.GetCachedAsync<ChaptersResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve chapter information for manga {id} with offset {offSet}",
                result.Error);

        var resp = result.Unwrap();

        if (resp.Total < resp.Limit + resp.Offset) return resp;

        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

        var extra = await GetChaptersForSeries(id, language, cancellationToken, resp.Limit + resp.Offset);

        resp.Data.AddRange(extra.Data);

        return resp;
    }

    private static List<ChapterData> FilterChapters(IList<ChapterData> chapters, string language,
        DownloadRequestDto request)
    {
        var scanlationGroup = request.GetStringOrDefault(RequestConstants.ScanlationGroupKey, string.Empty);
        var allowNonMatching = request.GetBool(RequestConstants.AllowNonMatchingScanlationGroupKey, true);

        return chapters
            .GroupBy(c => string.IsNullOrEmpty(c.Attributes.Chapter)
                ? string.Empty
                : $"{c.Attributes.Chapter} - {c.Attributes.Volume}")
            .SelectMany(g =>
            {
                if (string.IsNullOrEmpty(g.Key)) return g.ToList();

                var chapter = g.FirstOrDefault(ChapterFinder(language, scanlationGroup));

                if (chapter == null && allowNonMatching)
                    chapter = g.FirstOrDefault(ChapterFinder(language, string.Empty));

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
            if (!string.IsNullOrEmpty(chapter.Attributes.ExternalUrl)) return false;

            if (string.IsNullOrEmpty(scanlationGroup)) return true;

            return chapter.RelationShips.FirstOrDefault(r =>
            {
                if (r.Type != "scanlation_group" && r.Type != "user") return false;

                return r.Id == scanlationGroup;
            }) != null;
        };
    }

    private async Task<List<FormControlOption>> LoadTagOptions()
    {
        var result = await Client.GetCachedAsync<TagResponse>("/manga/tag", _cache);
        if (result.IsErr)
        {
            _logger.LogError(result.Error, "Failed to load tags");
            return [];
        }

        List<FormControlOption> options = [];
        foreach (var tagData in result.Unwrap().Data)
            if (tagData.Attributes.Name.TryGetValue("en", out var value))
                options.Add(FormControlOption.Option(value, tagData.Id));

        return options;
    }

    internal async Task<CoverResponse> GetCoverImages(string id, CancellationToken cancellationToken, int offset = 0)
    {
        var url = $"/cover?order[volume]=asc&limit=20&manga[]={id}&offset={offset}";

        var result = await Client.GetCachedAsync<CoverResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr) throw new MnemaException($"Failed to load cover images for {id}", result.Error);

        var resp = result.Unwrap();

        if (resp.Total < resp.Limit + resp.Offset) return resp;

        var extra = await GetCoverImages(id, cancellationToken, resp.Limit + resp.Offset);

        resp.Data.AddRange(extra.Data);

        return resp;
    }
}
