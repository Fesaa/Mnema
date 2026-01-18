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
using Mnema.Models.Entities.Content;

namespace Mnema.Services.Scheduled;

internal class MonitoredSeriesScheduler(
    ILogger<MonitoredSeriesScheduler> logger,
    IServiceScopeFactory scopeFactory,
    IRecurringJobManagerV2 recurringJobManager,
    IWebHostEnvironment environment
) : AbstractScheduler<MonitoredSeriesScheduler, MonitoredSeries>(logger, scopeFactory, recurringJobManager, environment), IMonitoredSeriesScheduler
{
    protected override string WatcherJobId => "monitored-releases.rss";
    protected override string WatcherDescription => "monitored releases watcher";

    protected override Task<List<MonitoredSeries>> GetEntitiesAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        return unitOfWork.MonitoredSeriesRepository
            .GetAllMonitoredSeries(cancellationToken);
    }

    protected override List<Provider> GetProviders(List<MonitoredSeries> entities)
    {
        return entities
            .SelectMany(m => m.Providers)
            .Distinct()
            .ToList();
    }

    protected override Task<ProcessResult> ProcessEntitiesAsync(IServiceScope scope, List<ContentRelease> releases, List<MonitoredSeries> entities, CancellationToken cancellationToken)
    {
        return ProcessMonitoredReleases(scope, releases, entities, cancellationToken);
    }

    public async Task<ProcessResult> ProcessMonitoredReleases(
        IServiceScope scope, List<ContentRelease> releases,
        List<MonitoredSeries> monitoredReleases, CancellationToken cancellationToken
    )
    {
        var monitoredSeriesService = scope.ServiceProvider.GetRequiredService<IMonitoredSeriesService>();

        HashSet<Guid> matchedMonitoredSeries = [];
        HashSet<string> actedOnIds = [];

        var processedDownloads = 0;
        var failedDownloads = 0;

        foreach (var release in releases)
        {
            // Do not start a download for the same monitored release twice
            var validMatches = monitoredReleases
                .Where(m => !matchedMonitoredSeries.Contains(m.Id))
                .ToList();

            var match = FindMatch(scope, validMatches, release);
            if (match == null) continue;

            try
            {
                if (await monitoredSeriesService.DownloadFromRelease(match, release, cancellationToken))
                {
                    matchedMonitoredSeries.Add(match.Id);
                    actedOnIds.Add(release.ReleaseId);
                    processedDownloads++;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error downloading content {Title} - {MonitoredSeriesId}", match.Title, match.Id);
                failedDownloads++;
            }

        }

        // This will include all releases, while only one per content is used.
        // This is correct as we don't want to start a new download for these. They'll have been downloaded already
        return new ProcessResult(
            releases.Where(r => actedOnIds.Contains(r.ReleaseId)).ToList(),
            processedDownloads,
            failedDownloads
        );
    }



    public static MonitoredSeries? FindMatch(IServiceScope scope, List<MonitoredSeries> monitoredReleases, ContentRelease release)
    {
        var parserService = scope.ServiceProvider.GetRequiredService<IParserService>();

        foreach (var monitoredRelease in monitoredReleases.Where(m => m.Providers.Contains(release.Provider)))
        {
            var parseResult = parserService.FullParse(release.ReleaseName, monitoredRelease.ContentFormat);

            if (parseResult.Series.Intersect(monitoredRelease.ValidTitles).Any())
                return monitoredRelease;
        }

        return null;
    }

}
