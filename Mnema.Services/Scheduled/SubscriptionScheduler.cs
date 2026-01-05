using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Services.Scheduled;

public class SubscriptionScheduler(
    ILogger<SubscriptionScheduler> logger,
    IServiceScopeFactory scopeFactory,
    IRecurringJobManagerV2 recurringJobManager,
    IWebHostEnvironment environment
) : ISubscriptionScheduler
{
    private const string WatcherJobId = "subscriptions.rss";

    private static readonly RecurringJobOptions RecurringJobOptions = new()
    {
        TimeZone = TimeZoneInfo.Local
    };

    public Task EnsureScheduledAsync()
    {
        const string cron = "*/15 * * * *";

        logger.LogDebug("Registering subscription watcher task with cron {cron}", cron);
        if (environment.IsDevelopment())
            recurringJobManager.RemoveIfExists(WatcherJobId);
        else
            recurringJobManager.AddOrUpdate<SubscriptionScheduler>(WatcherJobId,
                j => j.RunWatcher(),
                cron, RecurringJobOptions);
        return Task.CompletedTask;
    }

    public async Task RunWatcher()
    {
        using var scope = scopeFactory.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var downloadService = scope.ServiceProvider.GetRequiredService<IDownloadService>();

        var subscriptions = (await unitOfWork.SubscriptionRepository.GetAllSubscriptions())
            .Where(sub => sub.Status == SubscriptionStatus.Enabled)
            .ToList();
        if (subscriptions.Count == 0)
            return;

        var subsById = subscriptions
            .GroupBy(s => s.ContentId)
            .ToDictionary(g => g.Key, g => g.First());

        var providers = subscriptions
            .Select(s => s.Provider)
            .Distinct()
            .ToList();

        logger.LogTrace("Searching for recent updated for {Providers} providers", providers.Count);

        var (total, subscriptionsToStart) = await ProcessRecentlyUpdated(scope, providers, subsById);

        logger.LogInformation(
            "Found {Reports} updated reports, starting download for {ToStart}",
            total,
            subscriptionsToStart.Count
        );

        foreach (var subscription in subscriptionsToStart)
            await downloadService.StartDownload(subscription.AsDownloadRequestDto());
    }

    private async Task<(int, List<Subscription>)> ProcessRecentlyUpdated(
        IServiceScope scope, List<Provider> providers, Dictionary<string, Subscription> subsById
    )
    {
        var totalUpdated = 0;
        var subscriptionsToStart = new List<Subscription>();

        foreach (var provider in providers)
        {
            var repository = scope.ServiceProvider.GetKeyedService<IRepository>(provider);
            if (repository == null)
            {
                logger.LogWarning("Repository for {Provider} not found, while a subscription exists",
                    provider.ToString());
                continue;
            }

            var recentlyUpdated = await repository.GetRecentlyUpdated(CancellationToken.None);
            totalUpdated += recentlyUpdated.Count;

            var matching = recentlyUpdated
                .Where(release => subsById.TryGetValue(release.ContentId ?? string.Empty, out _))
                .Select(release => subsById[release.ContentId ?? string.Empty])
                .ToList();

            if (matching.Count == 0)
                continue;

            logger.LogDebug(
                "Found {Amount} subscriptions that have updated for provider {Provider}",
                matching.Count, provider
            );

            subscriptionsToStart.AddRange(matching);
        }

        return (totalUpdated, subscriptionsToStart);
    }
}
