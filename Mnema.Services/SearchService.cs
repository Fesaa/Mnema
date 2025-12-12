using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;

namespace Mnema.Services;

public class SearchService(ILogger<SearchService> logger, IServiceScopeFactory serviceScopeFactory): ISearchService
{

    public Task<PagedList<SearchResult>> Search(SearchRequest searchRequest, PaginationParams paginationParams, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var repository = scope.ServiceProvider.GetKeyedService<IRepository>(searchRequest.Provider.ToString());
        if (repository == null)
        {
            logger.LogWarning("No repository found for {Provider}, cannot search", searchRequest.Provider.ToString());
            throw new MnemaException($"Unsupported provider {searchRequest.Provider}");
        }

        return repository.SearchPublications(searchRequest, paginationParams, cancellationToken);
    }
}