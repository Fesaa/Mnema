using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Microsoft.Extensions.Caching.Distributed;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Comix;

public class ComixRepository(IHttpClientFactory clientFactory, IDistributedCache cache): IRepository
{

    private static readonly IMetadataKey<IEnumerable<string>> Status = MetadataKeys.Strings("status");
    private static readonly IMetadataKey<IEnumerable<string>> PublicationDemographic = MetadataKeys.Strings("publicationDemographic");
    private static readonly IMetadataKey<IEnumerable<string>> IncludedTags = MetadataKeys.Strings("includeTags");
    private static readonly IMetadataKey<IEnumerable<string>> ExcludedTags = MetadataKeys.Strings("excludeTags");

    private HttpClient Client => clientFactory.CreateClient(nameof(Provider.Comix));

    public async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        if (request.Query.Length < 3)
            throw new BadRequestException("Search query must be at least 3 characters long");

        var url = "api/v2/manga"
            .SetQueryParam("keyword", request.Query)
            .SetQueryParam("order[relevance]", "desc")
            .AddRange("statuses[]", request.GetKey(Status))
            .AddRange("demographics[]", request.GetKey(PublicationDemographic))
            .AddRange("genres[]", request.GetKey(IncludedTags))
            .SetQueryParam("genres_mode", "and")
            .AddRange("genres[]", request.GetKey(ExcludedTags).Select(g => $"-{g}"))
            .AddPagination(pagination.PageSize, pagination.PageNumber + 1); // 1 based

        var res = await Client.GetCachedAsync<ComixRespose<ComixPaginatedResult<ComixManga>>>(url, cache, cancellationToken: cancellationToken);
        if (res.IsErr)
            throw new MnemaException($"Failed to search: {res.Error?.Message}", res.Error);

        var data = res.Unwrap();
        if (data.Status != 200)
            throw new MnemaException($"Failed to search for comics, status code {data.Status}");

        var items = data.Result.Items.Select(m => new SearchResult
        {
            Id = m.HashId,
            DownloadUrl = null,
            Name = m.Title,
            Provider = Provider.Comix,
            Description = m.Synopsis,
            Size = m.Size(),
            Tags = [],
            Url = $"{Client.BaseAddress?.ToString()}title/{m.HashId}-{m.Slug}",
            ImageUrl = m.Poster.Large,
        });

        return new PagedList<SearchResult>(items, data.Result.Pagination.Total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        const string url = "api/v2/manga?order[chapter_updated_at]=desc&limit=50";

        var res = await Client.GetCachedAsync<ComixRespose<ComixPaginatedResult<ComixManga>>>(url, cache, cancellationToken: cancellationToken);
        if (res.IsErr)
            throw new MnemaException($"Failed to search for recently updated: {res.Error?.Message}", res.Error);

        var resp = res.Unwrap();
        if (resp.Status != 200)
            throw new MnemaException($"Failed to search for recently updated, status code {resp.Status}");

        return resp
            .Result
            .Items
            .Where(m => m.LatestChapter.HasValue)
            .Select(m => new ContentRelease
            {
                ReleaseId = m.HashId + "-" + m.LatestChapter!.Value.ToString(CultureInfo.InvariantCulture),
                DownloadUrl = string.Empty,
                Provider = Provider.Comix,
                ContentId = m.HashId,
                ReleaseName = m.Title,
                ContentName = string.Empty,
                ReleaseDate = DateTime.UtcNow,
            }).ToList();
    }

    public Task<List<FormControlDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = RequestConstants.LanguageKey.Key,
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
                Key = RequestConstants.ScanlationGroupKey.Key,
                Advanced = true,
                Type = FormType.Text
            },
            new FormControlDefinition
            {
                Key = RequestConstants.DownloadOneShotKey.Key,
                Type = FormType.Switch
            },
            new FormControlDefinition
            {
                Key = RequestConstants.IncludeCover.Key,
                Type = FormType.Switch,
                DefaultOption = "true"
            },
            new FormControlDefinition
            {
                Key = RequestConstants.TitleOverride.Key,
                Advanced = true,
                Type = FormType.Text
            },
            new FormControlDefinition
            {
                Key = RequestConstants.AllowNonMatchingScanlationGroupKey.Key,
                Advanced = true,
                Type = FormType.Switch,
                DefaultOption = "true"
            }
        ]);
    }



    public Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Type = FormType.MultiSelect,
                Key = Status.Key,
                Options = [
                    FormControlOption.Option("Ongoing", "releasing"),
                    FormControlOption.Option("Finished", "finished"),
                    FormControlOption.Option("Hiatus", "on_hiatus"),
                    FormControlOption.Option("Cancelled", "discontinued"),
                    FormControlOption.Option("Upcoming", "not_yet_released"),
                ]
            },
            new FormControlDefinition
            {
                Type = FormType.MultiSelect,
                Key = PublicationDemographic.Key,
                Options = [
                    FormControlOption.Option("Josei", "3"),
                    FormControlOption.Option("Seinen", "4"),
                    FormControlOption.Option("Shoujo", "1"),
                    FormControlOption.Option("Shounen", "2"),
                ]
            },
            new FormControlDefinition
            {
                Type = FormType.MultiSelect,
                Key = IncludedTags.Key,
                Options = ComixUtils.Genres
            },
            new FormControlDefinition
            {
                Type = FormType.MultiSelect,
                Key = ExcludedTags.Key,
                Options = ComixUtils.ExcludedGenres
            }
        ]);
    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var url = $"api/v2/manga/{request.Id}"
            .SetQueryParam("includes[]", "author")
            .AddRange("includes[]", ["artist", "genre"]);

        var res = await Client.GetCachedAsync<ComixRespose<ComixManga>>(url, cache, cancellationToken: cancellationToken);
        if (res.IsErr)
            throw new MnemaException($"Failed to retrieve information for manga {request.Id}: {res.Error?.Message}", res.Error);

        var resp = res.Unwrap();
        if (resp.Status != 200)
            throw new MnemaException($"Failed to retrieve information for manga {request.Id}, status code {resp.Status}");

        var language = request.GetKey(RequestConstants.LanguageKey);

        var manga = resp.Result;
        var chapters = await GetSeriesChapters(manga.HashId, cancellationToken: cancellationToken);

        var filteredChapters = FilterChapters(chapters, language, request).Select((chapter, idx) => new Chapter
        {
            Id = chapter.ChapterId.ToString(),
            Title = chapter.Name ?? string.Empty,
            VolumeMarker = chapter.Volume > 0 ? chapter.Volume.ToString() : string.Empty,
            ChapterMarker = chapter.Number.ToString(CultureInfo.InvariantCulture),
            SortOrder = idx,
            ReleaseDate = null,
            Tags = [],
            People = [],
            TranslationGroups = chapter.ScanlationGroup != null ? [chapter.ScanlationGroup.Name] : [],
        }).ToList();

        return new Series
        {
            Id = manga.HashId,
            Title = manga.Title,
            LocalizedSeries = manga.AltTitles.FirstOrDefault(),
            Summary = manga.Synopsis,
            CoverUrl = manga.Poster.Large,
            NonProxiedCoverUrl = null,
            RefUrl = $"{Client.BaseAddress?.ToString()}title/{manga.HashId}-{manga.Slug}",
            Status = PublicationStatus.Ongoing,
            TranslationStatus = null,
            Year = manga.Year,
            HighestVolumeNumber = manga.FinalVolume,
            HighestChapterNumber = manga.FinalChapter,
            AgeRating = manga.IsNsfw ? AgeRating.R18Plus : null,
            Tags = manga.Genres.Select(g => new Tag(g.Title, true)).ToList(),
            People = manga.Authors.Select(a => new Person { Name = a.Title, Roles = [PersonRole.Writer]})
                .Concat(manga.Authors.Select(a => new Person { Name = a.Title, Roles = [PersonRole.CoverArtist]}))
                .GroupBy(p => p.Name)
                .Select(g => new Person { Name = g.Key, Roles = g.SelectMany(p => p.Roles).Distinct().ToList()})
                .ToList(),
            Links = manga.Links.Links(),
            Chapters = filteredChapters
        };
    }

    private async Task<List<ComixChapter>> GetSeriesChapters(string id, int page = 1, CancellationToken cancellationToken = default)
    {
        var path = $"/manga/{id}/chapters";
        var hashToken = ComixUtils.GenerateHash(path);

        var url = "api/v2/" + path
            .SetQueryParam("order[number]", "desc")
            .SetQueryParam("limit", 100)
            .SetQueryParam("page", page)
            .SetQueryParam("time", 1)
            .SetQueryParam("_", hashToken);

        var res = await Client.GetCachedAsync<ComixRespose<ComixPaginatedResult<ComixChapter>>>(url, cache, cancellationToken: cancellationToken);
        if (res.IsErr)
            throw new MnemaException($"Failed to retrieve chapters for manga {id}: {res.Error?.Message}", res.Error);

        var resp = res.Unwrap();
        if (resp.Status != 200)
            throw new MnemaException($"Failed to retrieve chapters for manga {id}, status code {resp.Status}");

        var result = resp.Result;

        if (result.Pagination.TotalPages <= page) return result.Items;

        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

        var extra = await GetSeriesChapters(id, page + 1, cancellationToken);

        result.Items.AddRange(extra);

        return result.Items;
    }

    private static List<ComixChapter> FilterChapters(IList<ComixChapter> chapters, string language,
        DownloadRequestDto request)
    {
        var scanlationGroup = request.GetKey(RequestConstants.ScanlationGroupKey);
        var allowNonMatching = request.GetKey(RequestConstants.AllowNonMatchingScanlationGroupKey);

        return chapters
            .GroupBy(c => c.Number)
            .Select(g =>
            {
                var official = g.FirstOrDefault(c => c.Language == language && c.IsOfficial);
                if (official != null) return official;

                var chapter = g.FirstOrDefault(ChapterFinder(language, scanlationGroup));

                if (chapter == null && allowNonMatching)
                    chapter = g.FirstOrDefault(ChapterFinder(language, string.Empty));

                return chapter;
            })
            .WhereNotNull()
            .ToList();
    }

    private static Func<ComixChapter, bool> ChapterFinder(string language, string scanlationGroup)
    {
        return chapter =>
        {
            if (chapter.Language != language) return false;
            if (string.IsNullOrEmpty(scanlationGroup)) return true;

            if (chapter.ScanlationGroup == null) return false;

            return chapter.ScanlationGroup.Name == scanlationGroup || chapter.ScanlationGroup.Slug == scanlationGroup;
        };
    }

    public async Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        var url = $"/api/v2/chapters/{chapter.Id}";

        var res = await Client.GetCachedAsync<ComixRespose<ComixChapter>>(url, cache,
            cancellationToken: cancellationToken);
        if (res.IsErr)
            throw new MnemaException($"Failed to load chapter images: {res.Error?.Message}", res.Error);

        var resp = res.Unwrap();
        if (resp.Status != 200)
            throw new MnemaException($"Failed to load chapter images, status code {resp.Status}");

        return resp
            .Result
            .Images
            .Select(i => i.Url)
            .Select(uri => new DownloadUrl(uri, uri))
            .ToList();
    }
}
