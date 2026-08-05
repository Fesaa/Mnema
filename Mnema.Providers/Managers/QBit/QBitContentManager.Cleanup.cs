using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.API.External;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using QBittorrent.Client;

namespace Mnema.Providers.Managers.QBit;

internal partial class QBitContentManager
{

    private readonly ConcurrentDictionary<string, bool> _cleanupTorrents = [];

    [AutomaticRetry(Attempts = 0)]
    [Queue(HangfireQueue.TorrentCleanup)]
    [DisableConcurrentExecution(timeoutInSeconds: 86400 * 2)]
    public async Task CleanupTorrent(string hash, CancellationToken ct)
    {
        var (torrentInfo, externalDownloads) = await GetLinkedDownloads(hash, ct);
        if (externalDownloads.Count == 0)
        {
            _cleanupTorrents.TryRemove(hash, out _);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.GetRequiredService<IUnitOfWork>();
        var connectionService = scope.GetRequiredService<IConnectionService>();

        foreach (var externalDownload in externalDownloads)
        {
            if (externalDownload.IsErrored)
            {
                logger.LogTrace("Skipping over {Title} as it's in an errored state", externalDownload.Title);
                continue;
            }

            var sw = Stopwatch.StartNew();

            try
            {
                await CleanupExternalDownload(torrentInfo, externalDownload, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cleanup external download {Title} - {TorrentHash}", externalDownload.Title, externalDownload.ExternalId);

                var downloadInfo = new ExternalDownloadContent(externalDownload, torrentInfo).DownloadInfo;
                connectionService.CommunicateDownloadFailure(downloadInfo, ex);
            }
            finally
            {
                await unitOfWork.ExternalDownloadRepository.DeleteById(externalDownload.Id, ct);
            }

            logger.LogInformation("[{Title}/{Id}] Cleaned up in {Elapsed}",  externalDownload.Title, externalDownload.ExternalId, sw.Elapsed.ToReadableString());
        }

        _cleanupTorrents.TryRemove(torrentInfo.Hash, out _);
    }

    internal async Task CleanupExternalDownload(TorrentInfo torrent, ExternalDownload externalDownload, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var cleanupService = scope.GetRequiredService<ICleanupService>();
        var messageService = scope.GetRequiredService<IMessageService>();
        var connectionService = scope.GetRequiredService<IConnectionService>();

        var content = new ExternalDownloadContent(externalDownload, torrent);

        await cleanupService.CleanupAsync(content, ct);

        var monitoredSeriesId = externalDownload.GetKey(RequestConstants.MonitoredSeriesId);
        if (monitoredSeriesId != null)
        {
            BackgroundJob.Enqueue<IMonitoredSeriesService>(s => s.EnrichWithMetadata(monitoredSeriesId.Value, CancellationToken.None));
        }

        await messageService.DeleteContent(externalDownload.ExternalId);
        connectionService.CommunicateDownloadFinished(content.DownloadInfo);
    }

    internal async Task<(TorrentInfo torrentInfo, List<ExternalDownload> externalDownloads)> GetLinkedDownloads(string hash, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var externalDownloads = await unitOfWork.ExternalDownloadRepository.GetByExternalId(hash, ct);
        if (externalDownloads.Count == 0) return (null!, []);

        var query = new TorrentListQuery { Category = MnemaCategory, Hashes = [hash] };
        var torrents = await qBitClient.GetTorrentsAsync(query, ct);

        var torrentInfo = torrents.FirstOrDefault(t => t.Hash == hash);

        if (torrentInfo == null)
        {
            logger.LogWarning("Torrent to get no longer exists on the download client: {Id}", hash);

            await unitOfWork.ExternalDownloadRepository.DeleteByExternalId(hash, ct);
            return (null!, []);
        }

        return (torrentInfo, externalDownloads);
    }

}
