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
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;

namespace Mnema.Services;

internal class SearchService(ILogger<SearchService> logger, IServiceScopeFactory serviceScopeFactory,
    IConnectionService connectionService, IUnitOfWork unitOfWork, ISettingsService settingsService) : ISearchService
{
    public Task<PagedList<SearchResult>> Search(SearchRequest searchRequest, PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var repository = scope.ServiceProvider.GetKeyedService<IContentRepository>(searchRequest.Provider);
        if (repository == null)
        {
            logger.LogWarning("No repository found for {Provider}, cannot search", searchRequest.Provider.ToString());
            throw new BadRequestException($"Unsupported provider {searchRequest.Provider}");
        }

        return repository.Search(searchRequest, paginationParams, cancellationToken);
    }

    public async Task<List<ContentRelease>> SearchReleases(List<Provider> providers, CancellationToken cancellationToken)
    {
        var scope = serviceScopeFactory.CreateScope();

        List<ContentRelease> releases = [];

        foreach (var provider in providers)
        {
            var repository = scope.ServiceProvider.GetKeyedService<IContentRepository>(provider);
            if (repository == null)
            {
                logger.LogWarning("Repository for {Provider} not found, cannot find recently updated", provider.ToString());
                continue;
            }

            var providerSettings = await unitOfWork.ProviderSettingsRepository.GetSettingsForProvider(provider, cancellationToken);

            try
            {
                var recentlyUpdated = await GetRecentlyUpdated(provider, repository, cancellationToken);

                releases.AddRange(recentlyUpdated);
                providerSettings.Settings.SetKey(ProviderSettings.ConsecutiveFailures, 0);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to search for recently updated for {provider.ToString()}";

                var consecutiveFailures = providerSettings.Settings.Increment(ProviderSettings.ConsecutiveFailures);

                logger.LogError(ex, $"{errorMessage} - {consecutiveFailures} consecutive failures");

                var disableAfter = await settingsService.GetSettingsAsync<int>(ServerSettingKey.AutoDisableAfter);
                if (consecutiveFailures >= disableAfter)
                {
                    providerSettings.Settings.SetKey(ProviderSettings.Disable, true);
                    errorMessage += $" for {disableAfter} consecutive failures, disabling provider";
                }

                connectionService.CommunicateException(errorMessage, ex);
            }

            unitOfWork.ProviderSettingsRepository.Update(providerSettings);
        }

        await unitOfWork.CommitAsync(cancellationToken);

        return releases;
    }

    private async Task<IList<ContentRelease>> GetRecentlyUpdated(Provider provider, IContentRepository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            return await repository.GetRecentlyUpdated(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to search for recently updated {Provider}. Retrying once after 5s", provider.ToString());
        }

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        return await repository.GetRecentlyUpdated(cancellationToken);
    }
}
