using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Models.Publication;

namespace Mnema.Services;

public class MonitoredSeriesService(
    ILogger<MonitoredSeriesService> logger,
    IScannerService scannerService,
    IParserService parserService,
    IDownloadService downloadService,
    IMetadataResolver metadataResolver,
    ApplicationConfiguration configuration,
    IServiceProvider serviceProvider
): IMonitoredSeriesService
{
    public async Task<bool> DownloadFromRelease(MonitoredSeries monitoredSeries, ContentRelease release, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(release.DownloadUrl))
            return false;

        var contentManager = serviceProvider.GetKeyedService<IContentManager>(release.Provider);
        if (contentManager == null)
            return false;

        var series = await metadataResolver.ResolveSeriesAsync(monitoredSeries.Metadata, cancellationToken);

        var title = monitoredSeries.Metadata.GetString(RequestConstants.TitleOverride).OrNonEmpty(
            series?.Title,
            parserService.ParseSeries(release.ReleaseName, monitoredSeries.ContentFormat)
        );

        if (string.IsNullOrEmpty(title))
        {
            logger.LogWarning("Monotired series {Title} resulted in a empty title for release {Id}. Not downloading",
                monitoredSeries.Title, release.Id);
            return false;
        }

        var destDir = Path.Join(monitoredSeries.BaseDir, title);

        var onDiskContents = scannerService.ScanDirectory(destDir, monitoredSeries.ContentFormat,
            monitoredSeries.Format, cancellationToken);

        var chaptersInTorrent =
            await scannerService.ParseTorrentFile(release.DownloadUrl, monitoredSeries.ContentFormat,
                cancellationToken);

        var toDownloadFiles = chaptersInTorrent.Where(ShouldDownload).ToList();

        if (toDownloadFiles.Count == 0)
        {
            logger.LogDebug("No files to download for {Title} - {Id}", monitoredSeries.Title, release.Id);
            return false;
        }

        logger.LogDebug("Found {Count}/{TotalCount} chapters to download in {Title}", toDownloadFiles.Count,  chaptersInTorrent.Count, release.ReleaseName);


        await downloadService.StartDownload(new DownloadRequestDto
        {
            Provider = release.Provider,
            Id = release.ReleaseId,
            BaseDir = monitoredSeries.BaseDir,
            TempTitle = title,
            Metadata = monitoredSeries.Metadata,
            DownloadUrl = release.DownloadUrl,
            UserId = monitoredSeries.UserId,
            StartImmediately = false,
        });



        /*await contentManager.RelayMessage(new MessageDto
        {
            Provider = release.Provider,
            ContentId = release.ReleaseId,
            Type = MessageType.FilterContent,
            Data = JsonSerializer.Serialize(toDownloadFiles.Select(f => f.Id).ToList()),
        });*/

        await contentManager.RelayMessage(new MessageDto
        {
            Provider = release.Provider,
            ContentId = release.ReleaseId,
            Type = MessageType.StartDownload
        });

        return true;

        bool ShouldDownload(Chapter chapter)
        {
            if (string.IsNullOrEmpty(chapter.VolumeMarker) && string.IsNullOrEmpty(chapter.ChapterMarker))
            {
                logger.LogDebug("Skipping download for {Title} because it had no volume or chapter", chapter.Title);
                return false;
            }

            if (string.IsNullOrEmpty(chapter.ChapterMarker))
            {
                return !onDiskContents.Any(c => string.IsNullOrEmpty(c.Chapter) && c.Volume == chapter.VolumeMarker);
            }

            if (string.IsNullOrEmpty(chapter.VolumeMarker))
            {
                return !onDiskContents.Any(c => string.IsNullOrEmpty(c.Volume) && c.Chapter == chapter.ChapterMarker);
            }

            return !onDiskContents.Any(c => !string.IsNullOrEmpty(c.Volume)
                                           && !string.IsNullOrEmpty(c.Chapter)
                                           && c.Volume == chapter.VolumeMarker
                                           &&  c.Chapter == chapter.ChapterMarker);
        }
    }
}
