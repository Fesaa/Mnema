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
using Mnema.Common.Extensions;
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

        if (environment.IsDevelopment())
        {
            logger.LogDebug("Removing subscription watcher in development as recurring job");
            recurringJobManager.RemoveIfExists(WatcherJobId);
        }
        else
        {
            logger.LogDebug("Registering subscription watcher task with cron {cron}", cron);
            recurringJobManager.AddOrUpdate<SubscriptionScheduler>(WatcherJobId,
                j => j.RunWatcher(CancellationToken.None),
                cron, RecurringJobOptions);
        }

        return Task.CompletedTask;
    }

    public async Task RunWatcher(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var subscriptions = (await unitOfWork.SubscriptionRepository
                .GetAllSubscriptions(cancellationToken))
            .Where(sub => sub.Status == SubscriptionStatus.Enabled)
            .ToList();

        if (subscriptions.Count == 0)
            return;

        var providers = subscriptions
            .Select(s => s.Provider)
            .Distinct()
            .ToList();

        logger.LogTrace("Searching for recent updated for {Providers} providers", providers.Count);

        var releases = await FindReleases(scope, providers);
        if (releases.Count == 0)
        {
            logger.LogDebug("No releases found across {Providers} providers", providers.Count);
            return;
        }

        var oldestRelease = releases.Min(r => r.ReleaseDate);

        var processedReleases = await unitOfWork.ContentReleaseRepository
            .GetReleasesSince(oldestRelease, cancellationToken);

        var processedReleaseIds = processedReleases
            .Select(x => x.ContentId)
            .WhereNotNull()
            .Distinct()
            .ToHashSet();

        var newReleases = releases
            .Where(r => !string.IsNullOrEmpty(r.ContentId) && !processedReleaseIds.Contains(r.ContentId))
            .ToList();

        var startedSubs = await ProcessSubscriptions(scope, newReleases, subscriptions);

        logger.LogInformation(
            "Found {NewReports}/{TotalReports} reports, started {StartedDownloads} downloads",
            newReleases.Count,
            releases.Count,
            startedSubs
        );
    }

    private async Task<List<ContentRelease>> FindReleases(IServiceScope scope, List<Provider> providers)
    {
        List<ContentRelease> releases = [];

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

            releases.AddRange(recentlyUpdated);
        }

        return releases;
    }

    private static async Task<int> ProcessSubscriptions(
        IServiceScope scope, List<ContentRelease> releases, List<Subscription> subscriptions)
    {
        var downloadService = scope.ServiceProvider.GetRequiredService<IDownloadService>();

        var contentIds = releases
            .Select(x => x.ContentId)
            .WhereNotNull()
            .Distinct()
            .ToHashSet();

        var toStartSubs = subscriptions
            .Where(sub => contentIds.Contains(sub.ContentId))
            .DistinctBy(sub => sub.Id);

        var totalProcessed = 0;
        foreach (var sub in toStartSubs)
        {
            await downloadService.StartDownload(sub.AsDownloadRequestDto());
            totalProcessed++;
        }

        return totalProcessed;
    }
}
