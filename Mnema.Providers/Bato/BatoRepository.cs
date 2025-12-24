using System.Text.Json;
using System.Text.Json.Serialization;
using Fizzler.Systems.HtmlAgilityPack;
using Flurl;
using HtmlAgilityPack;
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

namespace Mnema.Providers.Bato;

internal sealed record BatoMapping([property: JsonPropertyName("file")] string File, [property: JsonPropertyName("text")]  string Text);
internal sealed record BatoSearchOptions(List<ModifierValueDto> Genres, List<ModifierValueDto> ReleaseStatus);

public class BatoRepository: IRepository
{

    private readonly AsyncLazy<BatoSearchOptions> _searchOptions;
    private readonly ILogger<BatoRepository> _logger;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private HttpClient Client => _httpClientFactory.CreateClient(nameof(Provider.Bato));

    private static readonly DistributedCacheEntryOptions LongCacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
    };

    public BatoRepository(ILogger<BatoRepository> logger, IDistributedCache cache, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _searchOptions = new AsyncLazy<BatoSearchOptions>(LoadSearchOptions);
    }
    
    public async Task<PagedList<SearchResult>> SearchPublications(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var includeGenres = string.Join(',', request.Modifiers.GetStrings("genres"));
        var excludeGenres = string.Join(',', request.Modifiers.GetStrings("ignored_genres"));
        var status = request.Modifiers.GetStringOrDefault("status", string.Empty);
        var upload = request.Modifiers.GetStringOrDefault("upload", string.Empty);

        var genreQuery = $"{includeGenres}|{excludeGenres}";
        
        var url = "/v3x-search".SetQueryParam("word", request.Query)
            .SetQueryParamIf(genreQuery != "|", "genres", genreQuery)
            .SetQueryParamIf(!string.IsNullOrEmpty(status), "status", status)
            .SetQueryParamIf(!string.IsNullOrEmpty(upload), "upload", upload)
            .SetQueryParam("page", Math.Max(1, pagination.PageNumber)); // Bato is 1 indexed
        
        var result = await Client.GetCachedStringAsync(url.ToString(), _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            _logger.LogError(result.Error, "Failed to retrieve search info with url {Url}", url);
            throw new MnemaException("Failed to search for series", result.Error);
        }

        var html = result.Unwrap();
        
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var seriesNodes = document.DocumentNode.QuerySelectorAll("div.border-b-base-200");
        if (seriesNodes == null)
        {
            return PagedList<SearchResult>.Empty();
        }

        var items = seriesNodes.Select(node =>
        {
            var id = node.QuerySelector("div > a")
                .GetAttributeValue("href", string.Empty)
                .RemovePrefix("/title/");
            
            var title = node.QuerySelector("h3 a span span")?.InnerText;
            if (string.IsNullOrEmpty(title))
            {
                title = node.QuerySelector("h3 a span")?.InnerText;
            }

            var imageUrl = node.QuerySelector("div > a > img")?.GetAttributeValue("src", string.Empty);
            var size = node.QuerySelector("a.link-hover.link-primary > span")?.InnerText;
            var tags = node.QuerySelectorAll("div.flex.flex-wrap.text-xs.opacity-70 > span > span:first-child")?
                .Select(n => n?.InnerText ?? string.Empty)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            return new SearchResult
            {
                Id = id,
                Name = title ?? "Unknown",
                Provider = Provider.Bato,
                ImageUrl = imageUrl,
                Url = $"{Client.BaseAddress?.ToString()}title/{id}",
                Size = size,
                Tags = tags ?? [],
            };
        }).ToList();

        if (items.Count == 0)
        {
            return PagedList<SearchResult>.Empty();
        }

        var paginatorNode = document.DocumentNode.SelectSingleNode("/html/body/div/main/div[4]");
        if (paginatorNode == null)
        {
            return new PagedList<SearchResult>(items, items.Count, 1, 1);
        }
        
        var lastPage = int.TryParse(paginatorNode.QuerySelectorAll("a").Last().InnerText, out var lastPageNumber) ? lastPageNumber : 0;
        var currentPage = int.TryParse(paginatorNode.QuerySelector("a.btn-accent").InnerText, out var pageNumber) ? pageNumber : 0;

        return new PagedList<SearchResult>(items, items.Count + (lastPage - 1) * 36, currentPage, 36);
    }

    public Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<DownloadMetadata> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult(new DownloadMetadata([
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
        var searchOptions = await _searchOptions;

        return [
            new ModifierDto
            {
                Title = "Included Genres",
                Type = ModifierType.Multi,
                Key = "genres",
                Values = searchOptions.Genres
            },
            new ModifierDto
            {
                Title = "Exclude Genres",
                Type = ModifierType.Multi,
                Key = "ignored_genres",
                Values = searchOptions.Genres
            },
            new ModifierDto
            {
                Title = "Publication status",
                Type = ModifierType.DropDown,
                Key = "status",
                Values = searchOptions.ReleaseStatus
            },
            new ModifierDto
            {
                Title = "Bato upload status",
                Type = ModifierType.DropDown,
                Key = "upload",
                Values = searchOptions.ReleaseStatus
            },
        ];
    }

    private async Task<BatoSearchOptions> LoadSearchOptions()
    {
        var html = (await Client.GetCachedStringAsync("/v3x-search", _cache, LongCacheEntryOptions)).UnwrapOr(string.Empty);
        if (string.IsNullOrEmpty(html))
        {
            _logger.LogWarning("No html found for Bato, cannot load search options");
            return new BatoSearchOptions([], []);
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var astroIsland = document.DocumentNode.SelectSingleNode("//astro-island[contains(@component-url, 'ClientTools')]");
        var clientTools = astroIsland.GetAttributeValue("component-url", string.Empty);
        if (string.IsNullOrEmpty(clientTools))
        {
            _logger.LogWarning("No ClientTools found for Bato, cannot load search options");
            return new BatoSearchOptions([], []);
        }

        var clientToolsJs = (await Client.GetCachedStringAsync(clientTools, _cache, LongCacheEntryOptions)).UnwrapOr(string.Empty);
        if (string.IsNullOrEmpty(clientToolsJs))
        {
            _logger.LogWarning("No ClientToolsJs found for Bato, cannot load search options");
            return new BatoSearchOptions([], []);
        }

        var genresImport = clientToolsJs.FindJsImport("content_comic_genres");
        var releaseStatusImport = clientToolsJs.FindJsImport("content_comic_release_statuss");

        if (string.IsNullOrEmpty(genresImport) || string.IsNullOrEmpty(releaseStatusImport))
            
        {
            _logger.LogWarning("Some imports where not found, cannot load search options");
            return new BatoSearchOptions([], []);
        }

        var genres = (await Client.GetCachedStringAsync($"/_astro/{genresImport.TrimStart('.', '/')}", _cache, LongCacheEntryOptions))
            .UnwrapOr(string.Empty);
        var releaseStatuses = (await Client.GetCachedStringAsync($"/_astro/{releaseStatusImport.TrimStart('.', '/')}", _cache, LongCacheEntryOptions))
            .UnwrapOr(string.Empty);

        if (string.IsNullOrEmpty(genres) || string.IsNullOrEmpty(releaseStatuses))
        {
            _logger.LogWarning("Some imports where not found, cannot load search options");
            return new BatoSearchOptions([], []);
        }

        var json = releaseStatuses.ExtractObjectLiteral().JsObjectToJson();
        var releaseStatusOptions = JsonSerializer.Deserialize<Dictionary<string, BatoMapping>>(json);
        if (releaseStatusOptions == null)
        {
            _logger.LogWarning("Failed to Deserialize releases, cannot load search options");
            return new BatoSearchOptions([], []);
        }
        
        json = genres.ExtractObjectLiteral().JsObjectToJson();
        var genresOptions = JsonSerializer.Deserialize<Dictionary<string, BatoMapping>>(json);
        if (genresOptions == null)
        {
            _logger.LogWarning("Failed to Deserialize genres, cannot load search options");
            return new BatoSearchOptions([], []);
        }

        return new BatoSearchOptions(
            genresOptions.Values.Select(si => ModifierValueDto.Option(si.File, si.Text)).ToList(),
            releaseStatusOptions.Values.Select(si => ModifierValueDto.Option(si.File, si.Text)).ToList()
            );
    }
}