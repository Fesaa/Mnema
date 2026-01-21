using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

namespace Mnema.Providers.Weebdex;

public class WeebdexRepository: IRepository
{

    private static readonly ConcurrentDictionary<string, string> LinkFormats =
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
    private readonly ILogger<WeebdexRepository> _logger;


    private readonly AsyncLazy<List<FormControlOption>> _tagOptions;

    public WeebdexRepository(ILogger<WeebdexRepository> logger, IDistributedCache cache,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _tagOptions = new AsyncLazy<List<FormControlOption>>(LoadTagOptions);
    }

    private HttpClient Client => _httpClientFactory.CreateClient(nameof(Provider.Weebdex));

    public async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = "/manga".SetQueryParam("title", request.Query)
            .AddRange("status", request.Modifiers.GetStrings("status"))
            .AddRange("contentRating", request.Modifiers.GetStrings("contentRating"))
            .AddRange("includedTags", request.Modifiers.GetStrings("includeTags"))
            .SetQueryParam("includedTagsMode", request.Modifiers.GetStringOrDefault("includedTagsMode", "AND"))
            .AddRange("excludedTags", request.Modifiers.GetStrings("excludeTags"))
            .SetQueryParam("excludedTagsMode", request.Modifiers.GetStringOrDefault("excludedTagsMode", "OR"))
            .AddPagination(pagination.PageSize, pagination.PageNumber + 1)
            .AddIncludes();

        var result =
            await Client.GetCachedAsync<SearchResponse>(url.ToString(), _cache, cancellationToken: cancellationToken);
        if (result.IsErr) throw new MnemaException("Failed to search for series", result.Error);

        var response = result.Unwrap();
        if (response.Data == null) return PagedList<SearchResult>.Empty();

        var items = response.Data.Select(sr => new SearchResult
        {
            Id = sr.Id,
            Name = sr.Title,
            Provider = Provider.Weebdex,
            Description = sr.Description,
            Tags = [],
            ImageUrl = $"https://weebdex.org/covers/{sr.Id}/{sr.Relationships.Cover.Id}{sr.Relationships.Cover.Ext}",
            Url = $"https://weebdex.org/title/{sr.Id}",
            Size = sr.Size(),
        });

        return new PagedList<SearchResult>(items, response.Total, response.Page - 1, response.Limit);
    }

    public async Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        var url = "chapter"
            .SetQueryParam("limit", 32)
            .SetQueryParam("offset", 0)
            .SetQueryParam("includes[]", "manga")
            .SetQueryParam("translatedLanguage[]", "en")
            .AddAllContentRatings()
            .SetQueryParam("order[readableAt]", "desc");

        var result = await Client.GetCachedAsync<ChapterResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException("Failed to load recently updated chapters", result.Error);

        return result.Unwrap()
            .Data
            .Select(chapter => new ContentRelease
            {
                ReleaseId = chapter.Id,
                ReleaseName = chapter.Title ?? string.Empty,
                ContentId = chapter.Relationships.Manga.Id,
                ReleaseDate = chapter.CreatedAt,
                Provider = Provider.Weebdex
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
        return [
            new FormControlDefinition
            {
                Key = "status",
                Type = FormType.MultiSelect,
                Options = Enum.GetValues<Status>()
                    .Select(s => FormControlOption.Option(s.ToString(), s))
                    .ToList()
            },
            new FormControlDefinition
            {
                Key = "contentRating",
                Type = FormType.MultiSelect,
                Options = Enum.GetValues<ContentRating>()
                    .Select(s => FormControlOption.Option(s.ToString(), s))
                    .ToList()
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

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var id = request.Id;
        var url = $"/manga/{id}".AddIncludes();

        var language = request.GetStringOrDefault(RequestConstants.LanguageKey, "en");

        var result =
            await Client.GetCachedAsync<Manga>(url.ToString(), _cache, cancellationToken: cancellationToken);
        if (result.IsErr) throw new MnemaException($"Failed to retrieve information for manga {id}", result.Error);

        var manga = result.Unwrap();
        var chapters = await GetChaptersForSeries(id, language, cancellationToken);

        var tags = manga.Relationships.Tags
            .Select(t => new Tag(t.Name, t.Group == "genre"))
            .ToList();

        var people = manga.Relationships.Authors
            .Select(a => new { a.Name, Role = PersonRole.Writer})
            .Concat(manga.Relationships.Artists
                .Select(a => new { a.Name, Role = PersonRole.Colorist}))
            .GroupBy(p => p.Name)
            .Select(g => new Person
            {
                Name = g.Key,
                Roles = g.Select(p => p.Role).Distinct().ToList()
            })
            .ToList();

        var filteredChapters = FilterChapters(chapters.Data, language, request).Select(chapter => new Chapter
        {
            Id = chapter.Id,
            Title = chapter.Title ?? string.Empty,
            VolumeMarker = chapter.Volume ?? string.Empty,
            ChapterMarker = chapter.ChapterNumber ?? string.Empty,
            ReleaseDate = chapter.PublishedAt,
            Tags = [],
            People = [],
            TranslationGroups = chapter.Relationships
                .Groups?
                .Select(g => g.Name)
                .ToList() ?? []
        }).ToList();

        return new Series
        {
            Id = manga.Id,
            Title = manga.Title,
            LocalizedSeries = manga.BestAltTitle(language),
            Summary = manga.Description,
            Status = manga.Status.AsPublicationStatus(),
            AgeRating = manga.ContentRating.AsAgeRating(),
            CoverUrl = $"https://weebdex.org/covers/{manga.Id}/{manga.Relationships.Cover.Id}{manga.Relationships.Cover.Ext}",
            HighestChapterNumber = manga.HighestChapter,
            HighestVolumeNumber = manga.HighestVolume,
            Year = manga.Year,
            Tags = tags,
            People = people,
            Links = manga.Relationships.Links?
                .Select(kv =>
                    LinkFormats.TryGetValue(kv.Key, out var format) ? string.Format(format, kv.Value) : string.Empty)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList() ?? [],
            Chapters = filteredChapters
        };
    }

    private async Task<ChapterResponse> GetChaptersForSeries(string id, string language,
        CancellationToken cancellationToken, int page = 1)
    {
        var url = $"/manga/{id}/chapters?order[volume]=desc&order[chapter]=desc"
            .AppendQueryParam("translatedLanguage[]", language)
            .AddPagination(20, page)
            .AddAllContentRatings();

        var result = await Client.GetCachedAsync<ChapterResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve chapter information for manga {id} with page {page}",
                result.Error);

        var resp = result.Unwrap();

        if (resp.Total < resp.Limit * resp.Page + 1) return resp;

        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

        var extra = await GetChaptersForSeries(id, language, cancellationToken, resp.Page+1);

        resp.Data.AddRange(extra.Data);

        return resp;
    }

    public async Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        var url = $"/chapter/{chapter.Id}";

        var result =
            await Client.GetCachedAsync<WeebdexChapter>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr) throw new MnemaException("Failed to retrieve chapter images", result.Error);

        var node = result.Unwrap().Node;

        return result.Unwrap().Data
            .Select(f => $"{node}/data/{chapter.Id}/{f.Name}")
            .Select(f => new DownloadUrl(f,f))
            .ToList();
    }

    private static List<WeebdexChapter> FilterChapters(IList<WeebdexChapter>? chapters, string language,
        DownloadRequestDto request)
    {
        if (chapters == null) return [];

        var scanlationGroup = request.GetStringOrDefault(RequestConstants.ScanlationGroupKey, string.Empty);
        var allowNonMatching = request.GetBool(RequestConstants.AllowNonMatchingScanlationGroupKey, true);

        return chapters
            .GroupBy(c => string.IsNullOrEmpty(c.ChapterNumber)
                ? string.Empty
                : $"{c.ChapterNumber} - {c.Volume}")
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

    private static Func<WeebdexChapter, bool> ChapterFinder(string language, string scanlationGroup)
    {
        return chapter =>
        {
            if (chapter.Language != language) return false;

            if (string.IsNullOrEmpty(scanlationGroup)) return true;

            return chapter.Relationships.Groups?
                .FirstOrDefault(r => r.Id == scanlationGroup) != null;
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
            options.Add(FormControlOption.Option(tagData.Name, tagData.Id));

        return options;
    }
}
