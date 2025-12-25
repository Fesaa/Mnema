using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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

    private static readonly Regex ChapterUrlRegex = new(@"https://[^""]+?\.webp", RegexOptions.Compiled,  TimeSpan.FromSeconds(5));
    private static readonly Regex BadServerRegex = new(@"k[0-9]{2}\.[a-z]+\.org", RegexOptions.Compiled,  TimeSpan.FromSeconds(5));
    private static readonly Regex CleanTitleRegex = new(@"[\(\[\{<«][^)\]\}>»]*[\)\]\}>»]", RegexOptions.Compiled,  TimeSpan.FromSeconds(5));

    private static readonly List<Regex> VolumeChapterRegexes =
    [
        new(@"(?:(?:Volume|Vol\.?) ?(\d+)\s+)?(?:Chapter|Ch\.?) ([\d\.]+)", RegexOptions.Compiled,  TimeSpan.FromSeconds(5)),
        new(@"(?:\[S(\d+)] ?)?Episode ([\d\.]+)", RegexOptions.Compiled,  TimeSpan.FromSeconds(5)),
    ];

    private static readonly Dictionary<string, List<PersonRole>> RoleMappings = new()
    {
        ["(Story&Art)"] = [PersonRole.Writer, PersonRole.Colorist],
        ["(Story)"] = [PersonRole.Writer],
        ["(Art)"] = [PersonRole.Colorist],
    };
    
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
            .SetQueryParam("page", pagination.PageNumber + 1); // Bato is 1 indexed
        
        var result = await Client.GetCachedStringAsync(url.ToString(), _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            _logger.LogError(result.Error, "Failed to retrieve search info with url {Url}", url);
            throw new MnemaException("Failed to search for series", result.Error);
        }

        var document = result.Unwrap().ToHtmlDocument();

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
            return new PagedList<SearchResult>(items, items.Count, 1, items.Count);
        }

        var lastPage = paginatorNode.QuerySelectorAll("a").Last().InnerText.AsInt();
        var currentPage = paginatorNode.QuerySelector("a.btn-accent").InnerText.AsInt();

        return new PagedList<SearchResult>(items, lastPage * 36, currentPage - 1, 36);
    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var result = await Client.GetCachedStringAsync($"/title/{request.Id}", _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            _logger.LogError(result.Error, "Failed to retrieve series info for {Id}", request.Id);
            throw new MnemaException("Failed to series info", result.Error);
        }
        
        var document = result.Unwrap().ToHtmlDocument();

        var titleNode = document.DocumentNode.SelectSingleNode("/html/body/div/main/div[1]/div[2]/div[1]/h3/a");
        if (titleNode == null)
            throw new MnemaException($"Series {request.Id} has no title");
        
        var title = titleNode.InnerText;

        List<string> summaryXPaths = [
            "/html/body/div/main/div[1]/div[2]/div[4]/astro-island/div/div[1]/astro-slot/div/astro-island[1]/div/div/div",
            "/html/body/div/main/div[1]/div[2]/div[5]/astro-island/div/div[1]/astro-slot/div/astro-island[1]/div/div/div"
        ];
        
        var summary = summaryXPaths
            .Select(xPath => document.DocumentNode.SelectSingleNode(xPath))
            .Select(n => n?.InnerText)
            .FirstOrDefault(n => !string.IsNullOrEmpty(n)) ??  string.Empty;

        var statusNode = document.DocumentNode.SelectSingleNode("/html/body/div/main/div[1]/div[2]/div[2]/div[3]/span[3]");
        var translationStatusNode =
            document.DocumentNode.SelectSingleNode("/html/body/div/main/div[1]/div[2]/div[2]/div[4]/span[3]");

        var genres = document.DocumentNode.SelectSingleNode("/html/body/div/main/div[1]/div[2]/div[2]/div[1]")
            .QuerySelectorAll("span > span:first-child")
            .Select(n => new Tag {Id = n.InnerText, Value = n.InnerText})
            .ToList();
        
        var people = document.DocumentNode
            .SelectSingleNode("/html/body/div/main/div[1]/div[2]/div[1]/div[2]")
            .QuerySelectorAll("a")
            .Select(n => ParsePerson(n?.InnerText))
            .WhereNotNull()
            .ToList();

        var chapterNodes = document.DocumentNode
            .QuerySelectorAll("[name=\"chapter-list\"] astro-slot > div");
        
        return new Series
        {
            Id = request.Id,
            RefUrl = $"{Client.BaseAddress?.ToString()}title/{request.Id}",
            Title = CleanTitleRegex.Replace(title, string.Empty).Trim(),
            Summary = summary,
            Status = TranslatePublicationStatus(statusNode?.InnerText) ?? PublicationStatus.Unknown,
            TranslationStatus = TranslatePublicationStatus(translationStatusNode?.InnerText),
            Tags = genres,
            People = people,
            Links = [],
            Chapters = chapterNodes?.Select(ParseChapter).ToList() ?? [],
        };

        PublicationStatus? TranslatePublicationStatus(string? publicationStatus)
        {
            if (string.IsNullOrEmpty(publicationStatus))
                return null;

            return publicationStatus switch
            {
                "pending" => PublicationStatus.Unknown,
                "ongoing" => PublicationStatus.Ongoing,
                "completed" => PublicationStatus.Completed,
                "hiatus" => PublicationStatus.Paused,
                "cancelled" => PublicationStatus.Cancelled,
                _ => PublicationStatus.Unknown,
            };
        }

        Chapter ParseChapter(HtmlNode node)
        {
            var linkNode = node.QuerySelector("div > a.link-hover.link-primary");
            var (volume, chapter) = ParseVolumeAndChapter(linkNode.InnerText);
                
            var chapterTitle = ParseTitle(linkNode.InnerText);
            if (string.IsNullOrEmpty(chapterTitle))
            {
                chapterTitle = node.QuerySelector("div > span.opacity-80")?.FirstChild?.InnerText.RemovePrefix(":").Trim();
            }

            // OneShot
            if (string.IsNullOrEmpty(chapterTitle) && string.IsNullOrEmpty(volume) && string.IsNullOrEmpty(chapter))
            {
                chapterTitle = linkNode.InnerText.Trim();
            }

            var translatorNode = node.QuerySelector("div.avatar > div > a")?.FirstChild;
            var translator = translatorNode?.GetAttributeValue("href", string.Empty)?.RemovePrefix("/u/");

            return new Chapter
            {
                Id = linkNode.GetAttributeValue("href", "").RemovePrefix("/title/"),
                Title = chapterTitle ?? string.Empty,
                VolumeMarker = volume,
                ChapterMarker = chapter,
                Tags = [],
                People = [],
                TranslationGroups = string.IsNullOrEmpty(translator) ? [] : [translator],
            };
        }

        Person? ParsePerson(string? person)
        {
            if (string.IsNullOrEmpty(person))
                return null;

            foreach (var mapping in RoleMappings.Where(mapping => person.Contains(mapping.Key)))
            {
                return new Person
                {
                    Name = person.Replace(mapping.Key, string.Empty).Trim(),
                    Roles = mapping.Value
                };
            }
            
            return new Person
            {
                Name = person.Trim(),
                Roles = [PersonRole.Writer]
            };
        }

        (string Volume, string Chapter) ParseVolumeAndChapter(string input)
        {
            foreach (var regex in VolumeChapterRegexes)
            {
                var match = regex.Match(input);
                if (!match.Success) continue;

                var volume = match.Groups.Count > 1 ? match.Groups[1].Value : "";
                var chapter = match.Groups.Count > 2 ? match.Groups[2].Value : "";

                return (volume, chapter);
            }
            
            return (string.Empty, string.Empty);
        }

        string ParseTitle(string input)
        {
            var idx = input.IndexOf(':');
            if (idx == -1) return string.Empty;
            
            if (idx + 1 == input.Length) return string.Empty;
            
            return input[idx..];
        }
    }

    public async Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        var result = await Client.GetCachedStringAsync($"/title/{chapter.Id}", _cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            _logger.LogError(result.Error, "Failed to retrieve chapter urls for chapter {Id}", chapter.Id);
            throw new MnemaException("Failed to retrieve chapter urls", result.Error);
        }
        
        var document = new HtmlDocument();
        document.LoadHtml(result.Unwrap());

        var astroIsland = document.DocumentNode.SelectSingleNode("/html/body/div/astro-island[1]");
        if (astroIsland == null)
            throw new MnemaException("No astro island found");
        
        var props = astroIsland.GetAttributeValue("props", string.Empty);
        if (string.IsNullOrEmpty(props))
            throw new MnemaException("No properties found");

        props = BadServerRegex.Replace(props, "n11.mbznp.org");

        return ChapterUrlRegex
            .Matches(props)
            .Select(match => new DownloadUrl(match.Value, match.Value))
            .ToList();
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

        var document = html.ToHtmlDocument();

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