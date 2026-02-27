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
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Services.Scheduled;

internal class MonitoredSeriesScheduler(
    ILogger<MonitoredSeriesScheduler> logger,
    IServiceScopeFactory scopeFactory,
    IRecurringJobManagerV2 recurringJobManager,
    IWebHostEnvironment environment
) : AbstractScheduler<MonitoredSeriesScheduler, MonitoredSeries>(logger, scopeFactory, recurringJobManager, environment)
{
    protected override string WatcherJobId => "monitored-releases.rss";
    protected override string WatcherDescription => "monitored releases watcher";

    protected override Task<List<MonitoredSeries>> GetEntitiesAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        return unitOfWork.MonitoredSeriesRepository.GetAll(MonitoredSeriesIncludes.Chapters, cancellationToken);
    }

    protected override List<Provider> GetProviders(List<MonitoredSeries> entities)
    {
        return entities
            .Select(m => m.Provider)
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
        var downloadService = scope.ServiceProvider.GetRequiredService<IDownloadService>();

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

            var match = await FindMatch(scope, validMatches, release, cancellationToken);
            if (match == null) continue;

            try
            {

                await downloadService.StartDownload(new DownloadRequestDto
                {
                    Provider = release.Provider,
                    Id = release.ContentId ?? release.ReleaseId,
                    BaseDir = match.BaseDir,
                    TempTitle = release.ContentName,
                    Metadata = match.MetadataForDownloadRequest(),
                    DownloadUrl = release.DownloadUrl,
                    StartImmediately = true,
                    UserId = match.UserId,
                });

                matchedMonitoredSeries.Add(match.Id);
                actedOnIds.Add(release.ReleaseId);
                processedDownloads++;
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

    public static async Task<MonitoredSeries?> FindMatch(IServiceScope scope, List<MonitoredSeries> monitoredReleases, ContentRelease release, CancellationToken ct)
    {
        var parserService = scope.ServiceProvider.GetRequiredService<IParserService>();
        var scannerService = scope.ServiceProvider.GetRequiredService<IScannerService>();

        foreach (var monitoredRelease in monitoredReleases.Where(m => m.Provider == release.Provider))
        {
            // Require exact match
            if (!string.IsNullOrEmpty(monitoredRelease.ExternalId))
            {
                if (monitoredRelease.ExternalId != release.ContentId) continue;

                return monitoredRelease;
            }


            var parseResult = parserService.FullParse(release.ReleaseName, monitoredRelease.ContentFormat);

            if (!parseResult.Series.Intersect(monitoredRelease.ValidTitles).Any())
                continue;

            // Ensure the release is in the correct format
            var (_, chapters) = await scannerService.ParseTorrentFile(release.DownloadUrl, monitoredRelease.ContentFormat, ct);
            var formats = chapters.Select(c => parserService.ParseFormat(c.Title));
            if (!formats.Contains(monitoredRelease.Format))
                continue;

            return monitoredRelease;
        }

        return null;
    }

}
