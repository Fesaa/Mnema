using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using QBittorrent.Client;

namespace Mnema.Providers.QBit;

internal partial class QBitContentManager
{

    public async Task DownloadTorrent(DownloadRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.DownloadUrl))
            return;

        var cFormat = request.Metadata.GetRequiredEnum<ContentFormat>(RequestConstants.ContentFormatKey);
        var format = request.Metadata.GetRequiredEnum<Format>(RequestConstants.FormatKey);

        var serviceProvider = scopeFactory.CreateScope().ServiceProvider;
        var metadataResolver = serviceProvider.GetRequiredService<IMetadataResolver>();
        var parserService = serviceProvider.GetRequiredService<IParserService>();
        var scannerService = serviceProvider.GetRequiredService<IScannerService>();

        var series = await metadataResolver.ResolveSeriesAsync([request.Provider], request.Metadata, ct);
        var title = request.Metadata.GetString(RequestConstants.TitleOverride)
            .OrNonEmpty(series?.Title, parserService.ParseSeries(request.TempTitle, cFormat));

        if (string.IsNullOrEmpty(title))
        {
            logger.LogWarning("[{Id}]Downloaded content has no title, aborting download", request.Id);
            return;
        }

        var existingContent =
            scannerService.ScanDirectory(Path.Join(request.BaseDir, title), cFormat, format, ct);
        var chapters = await scannerService.ParseTorrentFile(request.DownloadUrl, cFormat, ct);

        var toDownloadChapters = chapters.Where(ShouldDownload).ToList();
        if (toDownloadChapters.Count == 0)
        {
            logger.LogDebug("[{Title}/{Id}] no chapters to download, not starting", title, request.Id);
            return;
        }

        logger.LogDebug("[{Title}/{Id}] Found {Count}/{TotalCount} chapters to download",
            title, request.Id, toDownloadChapters.Count,  chapters.Count);

        var addRequest = new AddTorrentUrlsRequest(new Uri(request.DownloadUrl))
        {
            Category = MnemaCategory,
            Tags = [request.Provider.ToString()],
            DownloadFolder = Path.Join(configuration.DownloadDir, request.BaseDir, request.TempTitle),
            Paused = true,
        };

        await qBitClient.AddTorrentsAsync(addRequest, ct);
        await cache.SetAsJsonAsync(RequestCacheKey + request.Id, request, RequestCacheKeyOptions, token: ct);

        if (toDownloadChapters.Count != chapters.Count)
        {
            // Small delay to give qbit a bit of time to load everything
            // This should enough as all metadata is inside the .torrent file we're passing
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            // The full path is encoded as the title
            var paths = toDownloadChapters.Select(c => c.Title).ToList();
            await FilterContent(request.Id, paths, ct);
        }

        if (request.StartImmediately)
        {
            await qBitClient.ResumeTorrentsAsync([request.Id], ct);
        }

        return;

        bool ShouldDownload(Chapter chapter)
        {
            if (parserService.ParseFormat(chapter.Title) != format)
                return false;

            if (string.IsNullOrEmpty(chapter.VolumeMarker) && string.IsNullOrEmpty(chapter.ChapterMarker))
            {
                logger.LogDebug("Skipping download for {Title} because it had no volume or chapter", chapter.Title);
                return false;
            }

            if (string.IsNullOrEmpty(chapter.ChapterMarker))
            {
                return !existingContent.Any(c => string.IsNullOrEmpty(c.Chapter) && c.Volume == chapter.VolumeMarker);
            }

            if (string.IsNullOrEmpty(chapter.VolumeMarker))
            {
                return !existingContent.Any(c => string.IsNullOrEmpty(c.Volume) && c.Chapter == chapter.ChapterMarker);
            }

            return !existingContent.Any(c => c.Volume == chapter.VolumeMarker
                                             && c.Chapter == chapter.ChapterMarker
                                             && !string.IsNullOrEmpty(c.Volume)
                                             && !string.IsNullOrEmpty(c.Chapter));
        }
    }

}
