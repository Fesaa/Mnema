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

internal class MangadexRepository: IRepository
{

    private readonly AsyncLazy<List<ModifierValueDto>> _tagOptions;
    private readonly ILogger<MangadexRepository> _logger;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private HttpClient Client => _httpClientFactory.CreateClient(nameof(Provider.Mangadex));

    public MangadexRepository(ILogger<MangadexRepository> logger, IDistributedCache cache, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _tagOptions = new AsyncLazy<List<ModifierValueDto>>(LoadTagOptions);
    }

    public async Task<PagedList<SearchResult>> SearchPublications(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = "/manga".SetQueryParam("title", request.Query)
            .AddRange("status", request.Modifiers.GetStrings("status"))
            .AddRange("contentRating", request.Modifiers.GetStrings("contentRating"))
            .AddRange("publicationDemographic", request.Modifiers.GetStrings("publicationDemographic"))
            .AddRange("includedTags", request.Modifiers.GetStrings("includeTags"))
            .SetQueryParam("includedTagsMode", request.Modifiers.GetStringOrDefault("includedTagsMode", "AND"))
            .AddRange("excludedTags", request.Modifiers.GetStrings("excludeTags"))
            .SetQueryParam("excludedTagsMode", request.Modifiers.GetStringOrDefault("excludedTagsMode", "OR"))
            .AddPagination(pagination)
            .AddIncludes();
        
        var result = await Client.GetCachedAsync<SearchResponse>(url.ToString(), _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            throw new MnemaException("Failed to search for series", result.Error);
        }

        var response = result.Unwrap();
        if (response.Data == null)
        {
            return PagedList<SearchResult>.Empty();
        }
        
        _logger.LogDebug("Found {Amount} items out of {Total} for query {Query}", response.Data.Count, response.Total, request.Query);

        var items = response.Data.Select(searchResult => new SearchResult
        {
            Id = searchResult.Id,
            Name = searchResult.Attributes.LangTitle("en"),
            Provider = Provider.Mangadex,
            Description = searchResult.Attributes.Description.GetValueOrDefault("en"),
            Size = searchResult.Attributes.Size(),
            Tags = [],
            Url = searchResult.RefUrl,
            ImageUrl = searchResult.CoverUrl() ?? string.Empty,
        });

        return new PagedList<SearchResult>(items, response.Total, response.Offset / response.Limit, response.Limit);
    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var id = request.Id;
        var url = $"/manga/{id}".AddIncludes();

        var result = await Client.GetCachedAsync<MangaResponse>(url.ToString(), _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
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
            VolumeMarker = chapter.Attributes.Volume,
            ChapterMarker = chapter.Attributes.Chapter,
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
            CoverUrl = manga.CoverUrl(),
            Title = manga.Attributes.LangTitle(language),
            Summary = manga.Attributes.Description.GetValueOrDefault(language, string.Empty),
            Status = manga.Attributes.Status.AsPublicationStatus(),
            AgeRating = manga.Attributes.ContentRating.AsAgeRating(),
            Year = manga.Attributes.Year,
            HighestChapterNumber = manga.Attributes.HighestChapter,
            HighestVolumeNumber = manga.Attributes.HighestVolume,
            Links = [],
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

        var result = await Client.GetCachedAsync<ChaptersResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
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
    
    public async Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        var url = $"/at-home/server/{chapter.Id}";

        var result = await Client.GetCachedAsync<ChapterImagesResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            throw new MnemaException("Failed to retrieve chapter images", result.Error);
        }

        var imageInfo = result.Unwrap();
        var baseUrl = imageInfo.BaseUrl;
        var hash = imageInfo.Chapter.Hash;

        return imageInfo.Chapter.Data.Select(image =>
        {
            var preferredUrl = $"{baseUrl}/data/{hash}/{image}";
            // Mangadex is timing out on single chapter images. For these we'll get them from the fallback
            var fallbackUrl = $"https://uploads.mangadex.org/data/{hash}/{imageInfo}";

            return new DownloadUrl(preferredUrl, fallbackUrl);
        }).ToList();
    }

    public Task<DownloadMetadata> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult(new DownloadMetadata([
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.LanguageKey,
                FormType = FormType.Dropdown,
                DefaultOption = "en",
                Options = [
                    new KeyValue("en", "English"),
                    new KeyValue("zh", "Simplified Chinese"),
                    new KeyValue("zh-hk", "Traditional Chinese"),
                    new KeyValue("es", "Castilian Spanish"),
                    new KeyValue("fr", "French"),
                    new KeyValue("ja", "Japanese")
                ],
            },
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.ScanlationGroupKey,
                Advanced = true,
                FormType = FormType.Text,
            },
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.DownloadOneShotKey,
                FormType = FormType.Switch,
            },
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.IncludeCover,
                FormType = FormType.Switch,
                DefaultOption = "true",
            },
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.UpdateCover,
                Advanced = true,
                FormType = FormType.Switch,
            },
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.TitleOverride,
                Advanced = true,
                FormType = FormType.Text,
            },
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.AllowNonMatchingScanlationGroupKey,
                Advanced = true,
                FormType = FormType.Switch,
                DefaultOption = "true",
            }
        ]));
    }

    public async Task<List<ModifierDto>> Modifiers(CancellationToken cancellationToken)
    {
        return [
            new ModifierDto
            {
                Title = "Status",
                Type = ModifierType.Multi,
                Key = "status",
                Values = [
                    ModifierValueDto.Option("cancelled", "Cancelled"),
                    ModifierValueDto.Option("completed", "Completed"),
                    ModifierValueDto.Option("hiatus", "Hiatus"),
                    ModifierValueDto.Option("ongoing", "Ongoing"),
                ],
            },
            new ModifierDto
            {
                Title = "Content Rating",
                Type = ModifierType.Multi,
                Key = "contentRating",
                Values = [
                    ModifierValueDto.Option("safe", "Safe"),
                    ModifierValueDto.Option("suggestive", "Suggestive"),
                    ModifierValueDto.Option("erotica", "Erotica"),
                    ModifierValueDto.Option("pornographic", "Mature"),
                ],
            },
            new ModifierDto
            {
                Title = "Include Tags",
                Type = ModifierType.Multi,
                Key = "includeTags",
                Values = await _tagOptions,
            },
            new ModifierDto
            {
                Title = "Exclude Tags",
                Type = ModifierType.Multi,
                Key = "excludeTags",
                Values = await _tagOptions,
            },
            new ModifierDto
            {
                Title = "Tags inclusion mode",
                Type = ModifierType.DropDown,
                Key = "includeTagsMode",
                Values = [ModifierValueDto.DefaultValue("AND", "And"), ModifierValueDto.Option("OR", "Or")]
            },
            new ModifierDto
            {
                Title = "Tags exlusion mode",
                Type = ModifierType.DropDown,
                Key = "excludeTagsMode",
                Values = [ModifierValueDto.Option("AND", "And"), ModifierValueDto.DefaultValue("OR", "Or")]
            },
        ];
    }
    
    private async Task<List<ModifierValueDto>> LoadTagOptions()
    {
        var result = await Client.GetCachedAsync<TagResponse>("/manga/tag", _cache);
        if (result.IsErr)
        {
            _logger.LogError(result.Error, "Failed to load tags");
            return [];
        }

        List<ModifierValueDto> options = [];
        foreach (var tagData in result.Unwrap().Data)
        {
            if (tagData.Attributes.Name.TryGetValue("en", out var value))
            {
                options.Add(ModifierValueDto.Option(tagData.Id, value));
            }
        }

        return options;
    }

    internal async Task<CoverResponse> GetCoverImages(string id, CancellationToken cancellationToken, int offset = 0)
    {
        var url = $"/cover?order[volume]=asc&limit=20&manga[]={id}&offset={offset}";

        var result = await Client.GetCachedAsync<CoverResponse>(url, _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            throw new MnemaException($"Failed to load cover images for {id}", result.Error);
        }

        var resp = result.Unwrap();

        if (resp.Total < resp.Limit + resp.Offset)
        {
            return resp;
        }

        var extra = await GetCoverImages(id, cancellationToken, resp.Limit + resp.Offset);

        resp.Data.AddRange(extra.Data);

        return resp;
    }

}