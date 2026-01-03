using System.Globalization;
using System.Text.Json;
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
using Mnema.Providers.Bato;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Dynasty;

internal sealed record DynastyImage(string image);

internal class DynastyRepository(
    ILogger<BatoRepository> logger,
    IDistributedCache cache,
    IHttpClientFactory httpClientFactory)
    : IRepository
{
    
    private const string ChapterReleaseDateFormat = "MMM d, yyyy";
    private const string SeriesReleaseDateFormat = "MMM d \\'yy";
    private const int JsonOffset = 2;
    
    private static readonly Regex ChapterTitleRegex = new (@"Chapter\s+([\d.]+)(?::\s*(.+))?", RegexOptions.Compiled, TimeSpan.FromSeconds(5));
    
    private HttpClient Client => httpClientFactory.CreateClient(nameof(Provider.Dynasty));
    

    public async Task<PagedList<SearchResult>> SearchPublications(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = "/search"
            .SetQueryParam("q", request.Query)
            .SetQueryParamIf(request.Modifiers.GetBool("AllowChapters"), "classes[]", "Chapter")
            .AppendQueryParam("classes[]", "Series")
            .SetQueryParam("page", pagination.PageNumber + 1); // Dynasty is 1 indexed
        
        var result = await Client.GetCachedStringAsync(url.ToString(), cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            throw new MnemaException("Failed to search for series", result.Error);
        }

        var document = result.Unwrap().ToHtmlDocument();

        var resultNodes = document.DocumentNode.QuerySelectorAll(".chapter-list dd");
        if (resultNodes == null)
        {
            return PagedList<SearchResult>.Empty();
        }

        var baseUrl = Client.BaseAddress?.ToString().TrimEnd('/');
        
        var results = resultNodes.Select(node =>
        {
            var nameNode = node.QuerySelector(".name");

            return new SearchResult
            {
                Id = nameNode.GetAttributeValue("href", string.Empty),  
                Name = nameNode.InnerText,
                Provider = Provider.Dynasty,
                Tags = node.QuerySelectorAll(".tags a.label").Select(x => x.InnerText).ToList(),
                Url = $"{baseUrl}{nameNode.GetAttributeValue("href", string.Empty)}",
            };
        }).ToList();

        var paginatorNode = document.DocumentNode.QuerySelector(".pagination");
        if (paginatorNode == null)
        {
            return new PagedList<SearchResult>(results, results.Count, 0, results.Count);
        }

        var currentPage = paginatorNode.QuerySelector(".active a").InnerText.AsInt();
        var lastPage = paginatorNode.QuerySelectorAll("a").ElementAtOrDefault(^2)?.InnerText.AsInt() ?? 1;
        
        return new PagedList<SearchResult>(results, 20 * lastPage, currentPage - 1, 20);
    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var result = await Client.GetCachedStringAsync(request.Id, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            throw new MnemaException("Failed to retrieve series info", result.Error);
        }
        
        var document = result.Unwrap().ToHtmlDocument();

        if (request.Id.StartsWith("/series/"))
        {
            return ParseSeriesPage(document, request);
        }

        return ParseChapterPageAsSeries(document, request);

    }

    private Series ParseSeriesPage(HtmlDocument document, DownloadRequestDto request)
    {
        var coverUrl = document.DocumentNode.QuerySelector(".thumbnail")?.GetAttributeValue("src", string.Empty);
        var tags = document.DocumentNode.QuerySelectorAll(".tag-tags a")
            .Where(node => node.GetAttributeValue("href", string.Empty).StartsWith("/tags/"))
            .Select(node => new Tag(node.InnerText))
            .ToList();
        var people = document.DocumentNode.QuerySelectorAll(".tag-title a")
            .Where(node => node.GetAttributeValue("href", string.Empty).StartsWith("/authors/"))
            .Select(node => new Person
            {
                Name = node.InnerText,
                Roles = [PersonRole.Writer]
            })
            .ToList();
        
        
        return new Series
        {
            Id = request.Id,
            Title = document.DocumentNode.QuerySelector(".tag-title b").InnerText,
            LocalizedSeries = document.DocumentNode.QuerySelector(".aliases b")?.InnerText,
            Summary = document.DocumentNode.QuerySelector(".description p")?.InnerText ?? string.Empty,
            CoverUrl = string.IsNullOrEmpty(coverUrl) ? string.Empty : $"{Client.BaseAddress?.ToString().TrimEnd('/')}{coverUrl}",
            RefUrl =  $"{Client.BaseAddress?.ToString().TrimEnd('/')}{request.Id}",
            Status = ParseStatus(document.DocumentNode.QuerySelectorAll(".tag-title small")?.LastOrDefault()?.InnerText.RemovePrefix("— ")),
            Tags = tags,
            People = people,
            Links = [],
            Chapters = ParseChapters(document.DocumentNode.QuerySelector(".chapter-list")),
        };
    }

    private static List<Chapter> ParseChapters(HtmlNode parent)
    {
        List<Chapter> chapters = [];
        var currentVolume = "";
        
        foreach (var child in parent.Children())
        {
            if (child.Name == "dt")
            {
                if (child.InnerText.Contains("Volume"))
                {
                    currentVolume = child.InnerText.RemovePrefix("Volume").Trim();
                }
                continue;
            }

            if (child.Name != "dd")
            {
                continue;
            }

            var titleNode = child.QuerySelector(".name");
            
            var tags = child.QuerySelectorAll(".label")
                .Where(node => node.GetAttributeValue("href", string.Empty).StartsWith("/tags/"))
                .Select(node => new Tag(node.InnerText))
                .ToList();
            var (chapter, title) = ParseChapterAndTitle(titleNode.InnerText);
            
            chapters.Add(new Chapter
            {
                Id = titleNode.GetAttributeValue("href", string.Empty).RemovePrefix("/chapters/"),
                Title = title,
                VolumeMarker = currentVolume,
                ChapterMarker = chapter,
                Tags = tags,
                People = [],
                TranslationGroups = [],
                ReleaseDate = child.QuerySelector("small")?.InnerText.RemovePrefix("released").AsDateTime(SeriesReleaseDateFormat),
            });
        }

        return chapters;

        (string Chapter, string Title) ParseChapterAndTitle(string chapterText)
        {
            var matches = ChapterTitleRegex.Match(chapterText);
            if (matches is { Success: true, Groups.Count: 3 })
            {
                return (matches.Groups[1].Value, matches.Groups[2].Value);
            }

            return (string.Empty, chapterText);
        }
    }

    private Series ParseChapterPageAsSeries(HtmlDocument document, DownloadRequestDto request)
    {
        var title = document.DocumentNode.QuerySelector("#chapter-title b").InnerText;
        var tags = document.DocumentNode.QuerySelectorAll("#chapter-details .tags a")
            .Where(node => node.GetAttributeValue("href", string.Empty).StartsWith("/tags/"))
            .Select(node => new Tag(node.InnerText))
            .ToList();
        var people = document.DocumentNode.QuerySelectorAll("#chapter-title a")
            .Where(node => node.GetAttributeValue("href", string.Empty).StartsWith("/authors/"))
            .Select(node => new Person
            {
                Name = node.InnerText,
                Roles = [PersonRole.Writer]
            })
            .ToList();
        var releaseDate = document.DocumentNode.QuerySelector("#chapter-details .released")?.InnerText
            .Replace("  ", " ") // Dynasty sometimes has double spaces for some reason. Remove them 
            .AsDateTime(ChapterReleaseDateFormat);

        return new Series
        {
            Id = request.Id,
            Title = title,
            LocalizedSeries = document.DocumentNode.QuerySelector(".aliases b")?.InnerText,
            RefUrl = $"{Client.BaseAddress?.ToString().TrimEnd('/')}{request.Id}",
            Summary = string.Empty,
            Status = ParseStatus(document.DocumentNode.QuerySelectorAll(".tag-title small")?.LastOrDefault()?.InnerText.RemovePrefix("— ")),
            Year = releaseDate?.Year,
            Tags = tags,
            People = people,
            Links = [],
            Chapters = [
                new Chapter
                {
                    Id = request.Id.RemovePrefix("/chapters/"),
                    Title = title,
                    RefUrl = $"{Client.BaseAddress?.ToString()}{request.Id}",
                    VolumeMarker = string.Empty,
                    ChapterMarker = string.Empty,
                    Tags = tags,
                    People = people,
                    TranslationGroups = document.DocumentNode.QuerySelectorAll(".scanlators a").Select(node => node.InnerText.Trim()).ToList(),
                    ReleaseDate = releaseDate,
                }
            ],
        };
    }

    private static PublicationStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return PublicationStatus.Unknown;

        return status switch
        {
            "Completed" => PublicationStatus.Completed,
            "Ongoing" => PublicationStatus.Ongoing,
            _ => PublicationStatus.Unknown,
        };
    }

    public async Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        var result = await Client.GetCachedStringAsync($"chapters/{chapter.Id}", cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            throw new MnemaException($"Failed to retrieve chapter urls for {chapter.Id}", result.Error);
        }

        var document = result.Unwrap().ToHtmlDocument();

        var scriptNode = document.DocumentNode.QuerySelectorAll("script")
            .FirstOrDefault(node => node.InnerText.Contains("var pages"));
        if (scriptNode == null)
        {
            throw new MnemaException($"Failed to retrieve chapter urls for {chapter.Id}, no matching script found");
        }

        var start = scriptNode.InnerText.IndexOf("[{", StringComparison.InvariantCulture);
        var end = scriptNode.InnerText.LastIndexOf("}]", StringComparison.InvariantCulture);
        if (start == -1 || end == -1)
        {
            throw new MnemaException($"Failed to retrieve chapter urls for {chapter.Id}, could not find json data");
        }

        var jsonData = scriptNode.InnerText[start..(end + JsonOffset)];

        return JsonSerializer.Deserialize<List<DynastyImage>>(jsonData)?
            .Select(i => new DownloadUrl(i.image, i.image))
            .ToList() ?? [];
    }

    public async Task<IList<string>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        var result = await Client.GetCachedStringAsync(string.Empty, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve recently updated chapters", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        return document.DocumentNode.QuerySelectorAll(".chapter")
            .Select(node => node.GetAttributeValue("href", string.Empty))
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x =>
            {
                // Chapter belonging to a series end in _chXXX, transform into the series id 
                var idx = x.LastIndexOf("_ch", StringComparison.InvariantCulture);
                return idx < 0 ? x : x[..idx].Replace("chapters", "series");
            })
            .Distinct()
            .ToList();
    }

    public Task<DownloadMetadata> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult(new DownloadMetadata([
            new FormControlDefinition
            {
                Key = RequestConstants.DownloadOneShotKey,
                Type = FormType.Switch,
            },
            new FormControlDefinition
            {
                Key = RequestConstants.IncludeNotMatchedTagsKey,
                Type = FormType.Switch,
                Advanced = true,
            },
            new FormControlDefinition
            {
                Key = RequestConstants.IncludeCover,
                Type = FormType.Switch,
                DefaultOption = "true",
            },
            new FormControlDefinition
            {
                Key = RequestConstants.TitleOverride,
                Type = FormType.Text,
                Advanced = true,
            },
            new FormControlDefinition
            {
                Key = RequestConstants.SkipVolumeWithoutChapter,
                Type = FormType.Switch,
                Advanced = true,
            },
        ]));
    }

    public Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Type = FormType.Switch,
                Key = "AllowChapters",
                Options = [],
            }
        ]);
    }
}