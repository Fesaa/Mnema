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

public class DynastyRepository(
    ILogger<BatoRepository> logger,
    IDistributedCache cache,
    IHttpClientFactory httpClientFactory)
    : IRepository
{
    
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
            logger.LogError(result.Error, "Failed to retrieve search results with url {Url} ", url);
            throw new MnemaException("Failed to search for series", result.Error);
        }

        var document = result.Unwrap().ToHtmlDocument();

        var resultNodes = document.DocumentNode.QuerySelectorAll(".chapter-list dd");
        if (resultNodes == null)
        {
            return PagedList<SearchResult>.Empty();
        }
        
        var results = resultNodes.Select(node =>
        {
            var nameNode = node.QuerySelector(".name");

            return new SearchResult
            {
                Id = nameNode.GetAttributeValue("href", string.Empty),  
                Name = nameNode.InnerText,
                Provider = Provider.Dynasty,
                Tags = node.QuerySelectorAll(".tags a.label").Select(x => x.InnerText).ToList(),
                Url = $"{Client.BaseAddress?.ToString()}{nameNode.GetAttributeValue("href", string.Empty)}",
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
                Key = RequestConstants.DownloadOneShotKey,
                FormType = FormType.Switch,
            },
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.IncludeNotMatchedTagsKey,
                FormType = FormType.Switch,
                Advanced = true,
            },
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.IncludeCover,
                FormType = FormType.Switch,
                DefaultOption = "true",
            },
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.TitleOverride,
                FormType = FormType.Text,
                Advanced = true,
            },
            new DownloadMetadataDefinition
            {
                Key = RequestConstants.SkipVolumeWithoutChapter,
                FormType = FormType.Switch,
                Advanced = true,
            },
        ]));
    }

    public Task<List<ModifierDto>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<ModifierDto>>([
            new ModifierDto
            {
                Title = "Allow Chapters",
                Type = ModifierType.Switch,
                Key = "AllowChapters",
                Values = [],
            }
        ]);
    }
}