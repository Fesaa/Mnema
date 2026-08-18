using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;

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
                var errorMessage = $"Failed to search for recently updated for {provider.ToString()}: {ex.Message}";

                var consecutiveFailures = providerSettings.Settings.Increment(ProviderSettings.ConsecutiveFailures, 1);

                logger.LogError("{ErrorMessage} - {ConsecutiveFailures} consecutive failures", errorMessage, consecutiveFailures);

                var disableAfter = await settingsService.GetSettingsAsync<int>(ServerSettingKey.AutoDisableAfter);
                if (consecutiveFailures >= disableAfter && disableAfter != 0)
                {
                    providerSettings.Settings.SetKey(ProviderSettings.Disable, true);
                    errorMessage += $" for {disableAfter} consecutive failures, disabling provider";

                    connectionService.CommunicateProviderEnabledSwitch(provider);
                    BackgroundJob.Schedule(() => EnableProvider(provider, CancellationToken.None), TimeSpan.FromHours(2));
                }

                connectionService.CommunicateException(errorMessage, ex);
            }

            unitOfWork.ProviderSettingsRepository.Update(providerSettings);
        }

        await unitOfWork.CommitAsync(cancellationToken);

        return releases;
    }

    public async Task EnableProvider(Provider provider, CancellationToken cancellationToken)
    {
        var providerSettings = await unitOfWork.ProviderSettingsRepository.GetSettingsForProvider(provider, cancellationToken);

        providerSettings.Settings.SetKey(ProviderSettings.Disable, false);
        providerSettings.Settings.SetKey(ProviderSettings.ConsecutiveFailures, 0);
        unitOfWork.ProviderSettingsRepository.Update(providerSettings);

        await unitOfWork.CommitAsync(cancellationToken);

        connectionService.CommunicateProviderEnabledSwitch(provider);
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
            logger.LogError("Failed to search for recently updated {Provider} - {Exception}. Retrying once after 5s",
                provider.ToString(), ex.Message);
        }

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        return await repository.GetRecentlyUpdated(cancellationToken);
    }
}
