using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using QBittorrent.Client;

namespace Mnema.Providers.QBit;

internal partial class QBitContentManager
{

    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
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
        var connectionService = serviceProvider.GetRequiredService<IConnectionService>();
        var signalR = serviceProvider.GetRequiredService<IMessageService>();

        var series = await metadataResolver.ResolveSeriesAsync(request.Provider, request.Metadata, ct);
        var title = request.Metadata.GetString(RequestConstants.TitleOverride)
            .OrNonEmpty(series?.Title, parserService.ParseSeries(request.TempTitle, cFormat));

        if (string.IsNullOrEmpty(title))
        {
            logger.LogWarning("[{Id}]Downloaded content has no title, aborting download", request.Id);
            return;
        }

        var downloadDir = Path.Join(request.BaseDir, title);
        var existingContent = scannerService.ScanDirectory(downloadDir, cFormat, format, ct);
        var (size, chapters) = await scannerService.ParseTorrentFile(request.DownloadUrl, cFormat, ct);

        var toDownloadChapters = chapters.Where(c =>
        {
            var match = scannerService.FindMatch(existingContent, c);
            return match == null;
        }).ToList();

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

        var info = new DownloadInfo
        {
            Provider = request.Provider,
            Id = request.Id,
            ContentState = ContentState.Queued,
            Name = title,
            Description = series?.Summary,
            ImageUrl = series?.CoverUrl,
            RefUrl = series?.RefUrl,
            Size = string.Empty,
            TotalSize = size,
            Downloading = request.StartImmediately,
            Progress = 0,
            Estimated = 0,
            SpeedType = SpeedType.Bytes,
            Speed = 0,
            DownloadDir = downloadDir,
            UserId = request.UserId,
        };

        await signalR.AddContent(request.UserId, info);
        connectionService.CommunicateDownloadStarted(info);
    }

}
