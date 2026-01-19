using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.Content;

namespace Mnema.Services.Scheduled;

internal class SubscriptionScheduler(
    ILogger<SubscriptionScheduler> logger,
    IServiceScopeFactory scopeFactory,
    IRecurringJobManagerV2 recurringJobManager,
    IWebHostEnvironment environment
) : AbstractScheduler<SubscriptionScheduler, Subscription>(logger, scopeFactory, recurringJobManager, environment)
{
    protected override string WatcherJobId => "subscriptions.rss";
    protected override string WatcherDescription => "subscription watcher";

    protected override async Task<List<Subscription>> GetEntitiesAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        return (await unitOfWork.SubscriptionRepository
                .GetAllSubscriptions(cancellationToken))
            .Where(sub => sub.Status == SubscriptionStatus.Enabled)
            .ToList();
    }

    protected override List<Provider> GetProviders(List<Subscription> entities)
    {
        return entities
            .Select(s => s.Provider)
            .Distinct()
            .ToList();
    }

    protected override Task<ProcessResult> ProcessEntitiesAsync(IServiceScope scope, List<ContentRelease> releases, List<Subscription> entities, CancellationToken cancellationToken)
    {
        var downloadService = scope.ServiceProvider.GetRequiredService<IDownloadService>();

        return ProcessSubscriptions(downloadService, releases, entities, cancellationToken);
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
