using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Services;

internal class SearchService(ILogger<SearchService> logger, IServiceScopeFactory serviceScopeFactory) : ISearchService
{
    public Task<PagedList<SearchResult>> Search(SearchRequest searchRequest, PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var repository = scope.ServiceProvider.GetKeyedService<IContentRepository>(searchRequest.Provider);
        if (repository == null)
        {
            logger.LogWarning("No repository found for {Provider}, cannot search", searchRequest.Provider.ToString());
            throw new MnemaException($"Unsupported provider {searchRequest.Provider}");
        }

        return repository.Search(searchRequest, paginationParams, cancellationToken);
    }

    public async Task<List<ContentRelease>> SearchReleases(List<Provider> providers, CancellationToken cancellationToken)
    {
        var scope = serviceScopeFactory.CreateScope();

        List<ContentRelease> releases = [];

        foreach (var provider in providers.Where(p => p != Provider.Bato))
        {
            var repository = scope.ServiceProvider.GetKeyedService<IContentRepository>(provider);
            if (repository == null)
            {
                logger.LogWarning("Repository for {Provider} not found, cannot find recently updated", provider.ToString());
                continue;
            }

            try
            {
                var recentlyUpdated = await repository.GetRecentlyUpdated(cancellationToken);

                releases.AddRange(recentlyUpdated);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to search for recently updated {Provider}", provider.ToString());
            }

        }

        return releases;
    }
}
