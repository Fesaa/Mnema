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

internal sealed record ProcessResult(List<ContentRelease> Releases, int StartedDownloads, int FailedDownloads);

internal abstract class AbstractScheduler<TScheduler, TEntity>(
    ILogger<TScheduler> logger,
    IServiceScopeFactory scopeFactory,
    IRecurringJobManagerV2 recurringJobManager,
    IWebHostEnvironment environment
) where TScheduler : class
{
    protected abstract string WatcherJobId { get; }
    protected abstract string WatcherDescription { get; }
    protected virtual string CronExpression => "*/15 * * * *";

    private static readonly RecurringJobOptions RecurringJobOptions = new()
    {
        TimeZone = TimeZoneInfo.Local
    };

    public Task EnsureScheduledAsync()
    {
        if (environment.IsDevelopment())
        {
            logger.LogDebug("Removing {WatcherDescription} in development as recurring job", WatcherDescription);
            recurringJobManager.RemoveIfExists(WatcherJobId);
        }
        else
        {
            logger.LogDebug("Registering {WatcherDescription} task with cron {cron}", WatcherDescription, CronExpression);
            recurringJobManager.AddOrUpdate<TScheduler>(WatcherJobId,
                j => (j as AbstractScheduler<TScheduler, TEntity>)!.RunWatcher(CancellationToken.None),
                CronExpression, RecurringJobOptions);
        }

        return Task.CompletedTask;
    }

    public async Task RunWatcher(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        var entities = await GetEntitiesAsync(unitOfWork, cancellationToken);

        if (entities.Count == 0)
            return;

        var providers = GetProviders(entities);

        logger.LogTrace("Searching for recent updated for {ProviderCount} providers", providers.Count);

        var releases = await searchService.SearchReleases(providers, cancellationToken);
        if (releases.Count == 0)
        {
            logger.LogDebug("No releases found across {Providers} providers", providers.Count);
            return;
        }

        var newReleases = await FilterProcessedReleases(unitOfWork, releases, cancellationToken);

        var result = await ProcessEntitiesAsync(scope, newReleases, entities, cancellationToken);
        unitOfWork.ContentReleaseRepository.AddRange(result.Releases);

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
            result.StartedDownloads,
            result.FailedDownloads
        );
    }

    protected abstract Task<List<TEntity>> GetEntitiesAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken);

    protected abstract List<Provider> GetProviders(List<TEntity> entities);

    protected abstract Task<ProcessResult> ProcessEntitiesAsync(IServiceScope scope, List<ContentRelease> releases, List<TEntity> entities, CancellationToken cancellationToken);

    public static async Task<List<ContentRelease>> FilterProcessedReleases(IUnitOfWork unitOfWork,
        List<ContentRelease> releases, CancellationToken cancellationToken)
    {
        var releaseIds = releases.Select(r => r.ReleaseId).ToList();

        var newIds = await unitOfWork.ContentReleaseRepository
            .FilterReleases(releaseIds, cancellationToken);

        return releases.Where(r => newIds.Contains(r.ReleaseId)).ToList();
    }
}
