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
using Mnema.Models.Internal;
using Mnema.Models.Publication;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Webtoon;

internal class WebtoonRepository(
    ILogger<WebtoonRepository> logger,
    IDistributedCache cache,
    IHttpClientFactory httpClientFactory)
    : IRepository
{
    private HttpClient Client => httpClientFactory.CreateClient(nameof(Provider.Webtoons));

    public async Task<PagedList<SearchResult>> SearchPublications(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = ("en/search/" + request.Modifiers.GetStringOrDefault("search_type", "originals"))
            .SetQueryParam("keyword", request.Query)
            .SetQueryParam("page", pagination.PageNumber + 1); // Webtoons is 1 indexed

        var result = await Client.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);

        if (result.IsErr)
        {
            throw new MnemaException("Failed to search for webtoons", result.Error);
        }

        var baseUrl = Client.BaseAddress!.ToString();
        var document = result.Unwrap().ToHtmlDocument();

        var items = document.DocumentNode.QuerySelectorAll(".webtoon_list li a")
            .Select(node => new SearchResult
            {
                Id = node.GetAttributeValue("href", string.Empty).RemovePrefix(baseUrl),
                Name = node.QuerySelector(".title").InnerText,
                Provider = Provider.Webtoons,
                ImageUrl = $"/api/proxy/webtoon/covers/{node.QuerySelector("img")
                    .GetAttributeValue("src", string.Empty)
                    .RemovePrefix(SharedConstants.WebtoonImageBase)
                    .RemoveSuffix("?type=q90")}",
                Url = node.GetAttributeValue("href", string.Empty),
                Tags = [
                    node.QuerySelector(".view_count").InnerText,
                    ..node.QuerySelector(".author")?.InnerText
                        .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                      ?? []
                ]
            });

        var paginator = document.DocumentNode.QuerySelector("._pagination");
        var last = paginator?.QuerySelectorAll(".pagination").LastOrDefault()?.FirstChild.InnerText.AsInt();
        if (paginator == null || last == null)
        {
            var list = items.ToList();
            return new PagedList<SearchResult>(list, list.Count, 0, list.Count);
        }
        
        var current = paginator.QuerySelector("[aria-current=\"true\"]")?.InnerText.AsInt();

        return new PagedList<SearchResult>(items, 30 * last.Value, current - 1 ?? 0, 30);

    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var baseUrl = Client.BaseAddress!.ToString().TrimEnd('/');
        var url = $"{baseUrl}/{request.Id}";
        
        var result = await Client.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            throw new MnemaException($"Failed to get series info for {request.Id}", result.Error);
        }
        
        var document = result.Unwrap().ToHtmlDocument();

        var infoHeaderNode = document.DocumentNode.QuerySelector(".detail_header .info");
        var detailNode = document.DocumentNode.QuerySelector(".detail");
        
        var chapters = ParseChapters(document);
        var pages= ParsePages(document.DocumentNode);

        for (var index = 1; pages.Count > index; index++)
        {
            var pageUrl = baseUrl + pages[index];
            logger.LogDebug("Fetching page {pageUrl}", pages[index]);
            
            result = await Client.GetCachedStringAsync(pageUrl, cache, cancellationToken: cancellationToken);
            if (result.IsErr)
                throw new MnemaException($"Failed to get series info for {request.Id} on page {pageUrl}", result.Error);
            
            if (index == pages.Count - 1 && pages.Count > 10)
            {
                index = 1;
            }
            
            document = result.Unwrap().ToHtmlDocument();
            
            chapters.AddRange(ParseChapters(document));
            pages = ParsePages(document.DocumentNode);

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        return new Series
        {
            Id = request.Id,
            RefUrl = url,
            Title = infoHeaderNode.QuerySelector(".subj").InnerText.Trim('\n', '\t'),
            Summary = detailNode.QuerySelector(".summary")?.InnerText ?? string.Empty,
            Status = detailNode.QuerySelector(".day_info")?.InnerText.Contains("COMPLETED") ?? false
                ? PublicationStatus.Completed : PublicationStatus.Unknown,
            Tags = infoHeaderNode.QuerySelectorAll(".genre")?
                .Select(node => new Tag(node.InnerText, true))
                .ToList() ?? [],
            People = infoHeaderNode.QuerySelectorAll(".author_area a.author")?
                .Select(node => new Person
                {
                    Name = node.InnerText.Trim(),
                    Roles = [PersonRole.Writer]
                })
                .ToList() ?? [],
            Links = [],
            Chapters = chapters,
        };

        List<string> ParsePages(HtmlNode rootNode)
        {
            return rootNode.QuerySelectorAll(".paginate a")
                .Select(node => node.GetAttributeValue("href", string.Empty))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }
    }

    private List<Chapter> ParseChapters(HtmlDocument document)
    {
        return document.DocumentNode.QuerySelectorAll("._episodeItem > a").Select(node => new Chapter
        {
            Id = node.GetAttributeValue("data-episode-no", node.GetAttributeValue("href", string.Empty)),
            Title = node.QuerySelector(".subj span").InnerText,
            RefUrl = node.GetAttributeValue("href", string.Empty),
            CoverUrl = node.QuerySelector("span img")?.GetAttributeValue("src", string.Empty),
            VolumeMarker = string.Empty,
            ChapterMarker = node.QuerySelector(".tx")?.InnerText.RemovePrefix("#") ?? string.Empty,
            Tags = [],
            People = [],
            TranslationGroups = []
        }).ToList();
    }

    public async Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(chapter.RefUrl))
        {
            throw new MnemaException("Chapter URL is missing");
        }
        
        var result = await Client.GetCachedStringAsync(chapter.RefUrl, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
        {
            throw new MnemaException("Failed to retrieve chapter urls", result.Error);
        }
        
        var document = result.Unwrap().ToHtmlDocument();

        return document.DocumentNode.QuerySelectorAll("#_imageList img")
            .Select(node => node.GetAttributeValue("data-url", string.Empty))
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => new DownloadUrl(s, s))
            .ToList();
    }

    public Task<IList<string>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<string>>([]);
    }

    public Task<DownloadMetadata> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult(new DownloadMetadata([
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
            }
        ]));
    }

    public Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return  Task.FromResult(new List<FormControlDefinition>
        {
            new()
            {
                Type = FormType.DropDown,
                Key = "search_type",
                Options = [
                    FormControlOption.DefaultValue("originals", "Originals"),
                    FormControlOption.Option("canvas", "Canvas"),
                ]
            },
        });
    }
}