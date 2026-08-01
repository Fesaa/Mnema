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
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.Publication;

namespace Mnema.Services.Scheduled;

internal sealed record ProcessResult(List<ContentRelease> Releases, int StartedDownloads, int FailedDownloads);

internal class MonitoredSeriesScheduler(
    ILogger<MonitoredSeriesScheduler> logger,
    IServiceScopeFactory scopeFactory,
    IRecurringJobManagerV2 recurringJobManager,
    IWebHostEnvironment environment,
    IUnitOfWork unitOfWork
) : IScheduled
{
    private const string WatcherJobId = "monitored-releases.rss";
    private const string WatcherDescription = "monitored releases watcher";
    private const string CronExpression = "*/15 * * * *";

    private readonly RecurringJobOptions _recurringJobOptions = new()
    {
        TimeZone = TimeZoneInfo.Local
    };

    public Task EnsureScheduledAsync(CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment())
        {
            logger.LogDebug("Updating {WatcherDescription} in development as a monthly recurring job", WatcherDescription);
            recurringJobManager.AddOrUpdate<MonitoredSeriesScheduler>(WatcherJobId,
                j => j.RunWatcher(CancellationToken.None),
                "0 0 1 * *", _recurringJobOptions);
        }
        else
        {
            logger.LogDebug("Registering {WatcherDescription} task with cron {cron}", WatcherDescription, CronExpression);
            recurringJobManager.AddOrUpdate<MonitoredSeriesScheduler>(WatcherJobId,
                j => j.RunWatcher(CancellationToken.None),
                CronExpression, _recurringJobOptions);
        }

        return Task.CompletedTask;
    }

    public async Task RunWatcher(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        var entities = await unitOfWork.MonitoredSeriesRepository.GetAll(MonitoredSeriesIncludes.Chapters, cancellationToken);

        if (entities.Count == 0)
            return;

        var providers = await GetProviders(entities);

        logger.LogTrace("Searching for recent updated for {ProviderCount} providers", providers.Count);

        var releases = await searchService.SearchReleases(providers, cancellationToken);
        if (releases.Count == 0)
        {
            logger.LogDebug("No releases found across {Providers} providers", providers.Count);
            return;
        }

        var newReleases = await FilterProcessedReleases(unitOfWork, releases, cancellationToken);

        var result = await ProcessMonitoredReleases(scope, newReleases, entities, cancellationToken);
        unitOfWork.ContentReleaseRepository.AddRange(result.Releases);

        try
        {
            await unitOfWork.CommitAsync(cancellationToken);
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

    protected async Task<List<Provider>> GetProviders(List<MonitoredSeries> entities)
    {
        var providers = entities
            .Select(m => m.Provider)
            .Distinct()
            .ToList();

        var providerSettings = await unitOfWork.ProviderSettingsRepository.GetAllSettings(CancellationToken.None);
        var enabledProviders = providerSettings.Where(ps => ps.IsEnabled).Select(ps => ps.Provider).ToList();

        return providers.Where(enabledProviders.Contains).ToList();
    }

    private static async Task<List<ContentRelease>> FilterProcessedReleases(IUnitOfWork unitOfWork,
        List<ContentRelease> releases, CancellationToken cancellationToken)
    {
        var releaseIds = releases.Select(r => r.ReleaseId).ToList();

        var newIds = await unitOfWork.ContentReleaseRepository
            .FilterReleases(releaseIds, cancellationToken);

        return releases.Where(r => newIds.Contains(r.ReleaseId)).ToList();
    }

    public async Task<ProcessResult> ProcessMonitoredReleases(
        IServiceScope scope, List<ContentRelease> releases,
        List<MonitoredSeries> monitoredReleases, CancellationToken cancellationToken
    )
    {
        var downloadService = scope.ServiceProvider.GetRequiredService<IDownloadService>();
        var connectionService = scope.ServiceProvider.GetRequiredService<IConnectionService>();

        HashSet<Guid> matchedMonitoredSeries = [];
        HashSet<string> actedOnIds = [];
        HashSet<string> startedContent = [];

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
                if (string.IsNullOrEmpty(release.ContentId) || !startedContent.Contains(release.ContentId) || release.IsGroupedRelease)
                {

                    if (release.IsGroupedRelease || !await downloadService.HasContent(release.Provider, release.ContentId ?? release.ReleaseId))
                    {
                        var metadata = match.MetadataForDownloadRequest();
                        metadata.SetKey(RequestConstants.AllowPartialChapterData, true);
                        metadata.SetKey(RequestConstants.IsGroupedDownload, release.IsGroupedRelease);

                        await downloadService.StartDownload(new DownloadRequestDto
                        {
                            Provider = release.Provider,
                            Id = release.ContentId ?? release.ReleaseId,
                            BaseDir = match.BaseDir,
                            TempTitle = release.ContentName,
                            Metadata = metadata,
                            DownloadUrl = release.DownloadUrl,
                            StartImmediately = true,
                        });
                    }
                    else
                    {
                        logger.LogDebug("Content {Title} - {Id} is already being downloaded, not starting new download",
                            release.ContentName.OrNonEmpty(match.Title), release.ContentId ?? release.ReleaseId);
                    }
                }
                else
                {
                    logger.LogDebug("Skipping release {@Release}", release);
                }

                if (!string.IsNullOrEmpty(release.ContentId))
                {
                    startedContent.Add(release.ContentId);
                }

                matchedMonitoredSeries.Add(match.Id);
                actedOnIds.Add(release.ReleaseId);
                processedDownloads++;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error downloading content {Title} - {MonitoredSeriesId}", match.Title, match.Id);
                failedDownloads++;

                connectionService.CommunicateException($"Error starting automatic download {match.Title} - {match.Id} - {match.Provider}", e);
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
                if (monitoredRelease.ExternalId != release.ContentId)
                    continue;

                var chapter = monitoredRelease.Chapters
                    .FirstOrDefault(c => c.ExternalId == release.ReleaseId);

                if (chapter?.Status == MonitoredChapterStatus.NotMonitored)
                    return null;

                return monitoredRelease;
            }

            var toParseName = release.ContentName.OrNonEmpty(release.ReleaseName);
            var parseResult = parserService.FullParse(toParseName, monitoredRelease.ContentFormat);

            var hasTitleMatch = parseResult.Series.Any(seriesName =>
            {
                var normalizedSeriesName = seriesName.ToNormalized();

                return monitoredRelease.ValidTitles.Any(title => title.ToNormalized() == normalizedSeriesName);
            });

            if (!hasTitleMatch)
                continue;

            // Ensure the release is in the correct format
            var chapters = (await scannerService.ParseTorrentFile(release.DownloadUrl, ct)).Files
                .Select(f =>
                {
                    var chapterParseResult = parserService.FullParse(f.FileName, monitoredRelease.ContentFormat);

                    return new Chapter
                    {
                        Id = string.Empty,
                        Title = string.Empty,
                        FileName = f.FileName,
                        VolumeMarker = chapterParseResult.Volume.Value,
                        ChapterMarker = chapterParseResult.Chapter.Value,
                    };
                }).ToList();

            var formats = chapters.Select(c => parserService.ParseFormat(c.FileName)).ToList();
            if (!formats.Contains(monitoredRelease.Format))
                continue;

            var shouldSkipDownload = chapters
                .Select(chapter => parserService.FindMatch(monitoredRelease.Chapters, chapter))
                .All(c => c?.Status is MonitoredChapterStatus.Available or MonitoredChapterStatus.NotMonitored);

            if (shouldSkipDownload)
                continue;

            return monitoredRelease;
        }

        return null;
    }
}
