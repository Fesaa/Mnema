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

internal sealed record ProcessResult(List<ContentRelease> Releases, int StartedDownloads, int FailedDownloads);

internal class SubscriptionScheduler(
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

        var downloadService = scope.ServiceProvider.GetRequiredService<IDownloadService>();
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

        var releases = await FindReleases(scope, providers, cancellationToken);
        if (releases.Count == 0)
        {
            logger.LogDebug("No releases found across {Providers} providers", providers.Count);
            return;
        }

        var newReleases = await FilterProcessedReleases(unitOfWork, releases, cancellationToken);

        var subsResult = await ProcessSubscriptions(downloadService, newReleases, subscriptions, cancellationToken);
        unitOfWork.ContentReleaseRepository.AddRange(subsResult.Releases);

        try
        {
            await unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while saving processed releases to the database. Duplicate downloads may start. Report this!");
        }

        logger.LogInformation(
            "Found {TotalReleases} releases, {NewReleases} have not been processed. Started {StartedDownloads} downloads, {FailedDownloads} downloads failed",
            releases.Count,
            newReleases.Count,
            subsResult.StartedDownloads,
            subsResult.FailedDownloads
        );
    }

    public static async Task<List<ContentRelease>> FilterProcessedReleases(IUnitOfWork unitOfWork,
        List<ContentRelease> releases, CancellationToken cancellationToken)
    {
        var releaseIds = releases.Select(r => r.ReleaseId).ToList();

        var newIds = await unitOfWork.ContentReleaseRepository
            .FilterReleases(releaseIds, cancellationToken);

        return releases.Where(r => newIds.Contains(r.ReleaseId)).ToList();
    }

    public async Task<List<ContentRelease>> FindReleases(
        IServiceScope scope, List<Provider> providers, CancellationToken cancellationToken)
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

            try
            {
                var recentlyUpdated = await repository.GetRecentlyUpdated(cancellationToken);

                releases.AddRange(recentlyUpdated);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to search for recently updated releases: {Provider}", provider.ToString());
            }
        }

        return releases;
    }

    public async Task<ProcessResult> ProcessSubscriptions(
        IDownloadService downloadService, List<ContentRelease> releases,
        List<Subscription> subscriptions, CancellationToken cancellationToken)
    {
        var contentIds = releases
            .Select(x => x.ContentId)
            .WhereNotNull()
            .Distinct()
            .ToHashSet();

        var toStartSubs = subscriptions
            .Where(sub => contentIds.Contains(sub.ContentId))
            .DistinctBy(sub => sub.Id);
        var actedOnIds = new HashSet<string?>();

        var processedSubs = 0;
        var failedSubs = 0;

        foreach (var sub in toStartSubs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await downloadService.StartDownload(sub.AsDownloadRequestDto());

                actedOnIds.Add(sub.ContentId);
                processedSubs++;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error downloading content {ContentId}", sub.ContentId);
                failedSubs++;
            }
        }

        // This will include all releases, while only one per content is used.
        // This is correct as we don't want to start a new download for these. They'll have been downloaded already
        return new ProcessResult(
            releases.Where(r => actedOnIds.Contains(r.ContentId)).ToList(),
            processedSubs,
            failedSubs
            );
    }
}
