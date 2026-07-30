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
using Mnema.Models.Entities.Content;
using QBittorrent.Client;

namespace Mnema.Providers.Managers.QBit;

internal partial class QBitContentManager
{

    private readonly ConcurrentDictionary<string, bool> _cleanupTorrents = [];

    private void EnqueueForCleanup(ExternalDownloadContent torrent)
    {
        if (!_cleanupTorrents.TryAdd(torrent.Id, true)) return;

        BackgroundJob.Enqueue(() => CleanupTorrent(torrent.Id, CancellationToken.None));
    }

    [AutomaticRetry(Attempts = 0)] // Do not retry this we should be handling all meaningful errors
    [Queue(HangfireQueue.TorrentCleanup)]
    [DisableConcurrentExecution(timeoutInSeconds: 86400 * 2)] // 2 days
    public async Task CleanupTorrent(string hash, CancellationToken ct)
    {
        var infos = await GetTorrent(hash, ct);
        if (infos.externalDownloads.Count == 0)
        {
            _cleanupTorrents.TryRemove(hash, out _);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.GetRequiredService<IUnitOfWork>();

        foreach (var externalDownload in infos.externalDownloads)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await CleanupExternalDownload(infos.torrentInfo, externalDownload, ct);

                await unitOfWork.ExternalDownloadRepository.DeleteById(externalDownload.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cleanup external download {Id} - {TorrentHash}",
                    externalDownload.Id, externalDownload.ExternalId);
            }

            logger.LogInformation("[{Title}/{Id}] Cleaned up in {Elapsed}ms",  externalDownload.Title, externalDownload.ExternalId, sw.ElapsedMilliseconds);
        }

        _cleanupTorrents.TryRemove(infos.torrentInfo.Hash, out _);
    }

    private async Task CleanupExternalDownload(TorrentInfo torrent, ExternalDownload externalDownload, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var content = new ExternalDownloadContent(externalDownload, torrent);

        await scope.GetRequiredService<ICleanupService>().CleanupAsync(content, ct);

        var monitoredSeriesId = externalDownload.GetKey(RequestConstants.MonitoredSeriesId);
        if (monitoredSeriesId != null)
        {
            BackgroundJob.Enqueue<IMonitoredSeriesService>(s => s.EnrichWithMetadata(monitoredSeriesId.Value, CancellationToken.None));
        }

        await scope.GetRequiredService<IMessageService>().DeleteContent(externalDownload.UserId, externalDownload.ExternalId);
        scope.GetRequiredService<IConnectionService>().CommunicateDownloadFinished(content.DownloadInfo);
    }

    private async Task<(TorrentInfo torrentInfo, List<ExternalDownload> externalDownloads)> GetTorrent(string hash, CancellationToken ct)
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
