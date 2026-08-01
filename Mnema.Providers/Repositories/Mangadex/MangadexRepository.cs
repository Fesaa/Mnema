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
using Mnema.Models.Enums;
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

    private static readonly IMetadataKey<IEnumerable<string>> Status = MetadataKeys.Strings("status");
    private static readonly IMetadataKey<IEnumerable<string>> ContentRating = MetadataKeys.Strings("contentRating", ["safe", "suggestive", "erotica"]);
    private static readonly IMetadataKey<IEnumerable<string>> PublicationDemographic = MetadataKeys.Strings("publicationDemographic");
    private static readonly IMetadataKey<IEnumerable<string>> IncludedTags = MetadataKeys.Strings("includeTags");
    private static readonly IMetadataKey<string> IncludedTagsMode = MetadataKeys.String("includeTagsMode", "AND");
    private static readonly IMetadataKey<IEnumerable<string>> ExcludedTags = MetadataKeys.Strings("excludeTags");
    private static readonly IMetadataKey<string> ExcludedTagsMode = MetadataKeys.String("excludeTagsMode", "OR");

    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MangadexRepository> _logger;


    private readonly AsyncLazy<List<SelectOption<string>>> _tagOptions;

    public MangadexRepository(ILogger<MangadexRepository> logger, IDistributedCache cache,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _tagOptions = new AsyncLazy<List<SelectOption<string>>>(LoadTagOptions);
    }

    private HttpClient Client => _httpClientFactory.CreateClient(nameof(Provider.Mangadex));

    public async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        var url = "/manga"
            .SetQueryParam("title", request.Query)
            .AddRange("status[]", request.Modifiers.GetKey(Status))
            .AddRange("publicationDemographic[]", request.Modifiers.GetKey(PublicationDemographic))
            .AddRange("includedTags[]", request.Modifiers.GetKey(IncludedTags))
            .SetQueryParam("includedTagsMode", request.Modifiers.GetKey(IncludedTagsMode))
            .AddRange("excludedTags[]", request.Modifiers.GetKey(ExcludedTags))
            .SetQueryParam("excludedTagsMode", request.Modifiers.GetKey(ExcludedTagsMode))
            .AddRange("contentRating[]", request.Modifiers.GetKey(ContentRating))
            .AddOffsetPagination(pagination)
            .AddIncludes();

        var result =
            await Client.GetCachedAsync<SearchResponse>(url.ToString(), _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to search for series: {result.Error?.Message}", result.Error);

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
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve information for manga {id}: {result.Error?.Message}", result.Error);

        var language = request.GetKey(RequestConstants.LanguageKey);

        var manga = result.Unwrap().Data;
        var originalLanguage = manga.Attributes.OriginalLanguage;

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
            LocalizedSeries = string.IsNullOrEmpty(originalLanguage) ? null : manga.Attributes.LangTitle(originalLanguage),
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

    public async Task<IList<DownloadUrl>> ChapterUrls(MetadataBag metadata, Chapter chapter,
        CancellationToken cancellationToken)
    {
        var url = $"/at-home/server/{chapter.Id}";

        var result =
            await Client.GetCachedAsync<ChapterImagesResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve chapter images: {result.Error?.Message}", result.Error);

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
            throw new MnemaException($"Failed to load recently updated chapters: {result.Error?.Message}", result.Error);

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

    public Task<List<FormFieldDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormFieldDefinition>>([
            new DropDownFieldDefinition<string>
            {
                Key = RequestConstants.LanguageKey.Key,
                DefaultValue = "en",
                Options =
                [
                    SelectOption<string>.FromString("en"),
                    SelectOption<string>.FromString("zh"),
                    SelectOption<string>.FromString("zh-hk"),
                    SelectOption<string>.FromString("es"),
                    SelectOption<string>.FromString("fr"),
                    SelectOption<string>.FromString("ja")
                ]
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.ScanlationGroupKey.Key,
                Advanced = true,
            },
            new SwitchFieldDefinition
            {
                Key = RequestConstants.DownloadOneShotKey.Key,
            },
            new SwitchFieldDefinition
            {
                Key = RequestConstants.IncludeCover.Key,
                DefaultValue = true
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.TitleOverride.Key,
                Advanced = true,
            },
            new SwitchFieldDefinition
            {
                Key = RequestConstants.AllowNonMatchingScanlationGroupKey.Key,
                Advanced = true,
                DefaultValue = true,
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.HardcoverSeriesIdKey.Key,
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.MangaBakaKey.Key,
            }
        ]);
    }

    public async Task<List<FormFieldDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return
        [
            new MultiSelectFieldDefinition<string>
            {
                Key = Status.Key,
                Options =
                [
                    SelectOption<string>.Option("Cancelled", "cancelled"),
                    SelectOption<string>.Option("Completed", "completed"),
                    SelectOption<string>.Option("Hiatus", "hiatus"),
                    SelectOption<string>.Option("Ongoing", "ongoing")
                ]
            },
            new MultiSelectFieldDefinition<string>
            {
                Key = ContentRating.Key,
                Options =
                [
                    SelectOption<string>.Option("Safe", "safe"),
                    SelectOption<string>.Option("Suggestive", "suggestive"),
                    SelectOption<string>.Option("Erotica", "erotica"),
                    SelectOption<string>.Option("Pornographic", "pornographic")
                ]
            },
            new MultiSelectFieldDefinition<string>
            {
                Key = IncludedTags.Key,
                Options = await _tagOptions
            },
            new MultiSelectFieldDefinition<string>
            {
                Key = ExcludedTags.Key,
                Options = await _tagOptions
            },
            new DropDownFieldDefinition<string>
            {
                Key = IncludedTagsMode.Key,
                Options = [SelectOption<string>.DefaultOption("And", "AND"), SelectOption<string>.Option("Or", "OR")]
            },
            new DropDownFieldDefinition<string>
            {
                Key = ExcludedTagsMode.Key,
                Options = [SelectOption<string>.Option("And", "AND"), SelectOption<string>.DefaultOption("Or", "OR")]
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
            throw new MnemaException($"Failed to retrieve chapter information for manga {id} with offset {offSet}: {result.Error?.Message}",
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
        var scanlationGroup = request.GetKey(RequestConstants.ScanlationGroupKey);
        var allowNonMatching = request.GetKey(RequestConstants.AllowNonMatchingScanlationGroupKey);

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

    private async Task<List<SelectOption<string>>> LoadTagOptions()
    {
        var result = await Client.GetCachedAsync<TagResponse>("/manga/tag", _cache);
        if (result.IsErr)
        {
            _logger.LogError(result.Error, "Failed to load tags");
            return [];
        }

        List<SelectOption<string>> options = [];
        foreach (var tagData in result.Unwrap().Data)
            if (tagData.Attributes.Name.TryGetValue("en", out var value))
                options.Add(SelectOption<string>.Option(value, tagData.Id));

        return options;
    }

    internal async Task<CoverResponse> GetCoverImages(string id, CancellationToken cancellationToken, int offset = 0)
    {
        var url = $"/cover?order[volume]=asc&limit=20&manga[]={id}&offset={offset}";

        var result = await Client.GetCachedAsync<CoverResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to load cover images for {id}: {result.Error?.Message}", result.Error);

        var resp = result.Unwrap();

        if (resp.Total < resp.Limit + resp.Offset) return resp;

        var extra = await GetCoverImages(id, cancellationToken, resp.Limit + resp.Offset);

        resp.Data.AddRange(extra.Data);

        return resp;
    }
}
