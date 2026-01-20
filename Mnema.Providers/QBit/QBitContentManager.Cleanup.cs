using System;
using System.Collections.Concurrent;
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

namespace Mnema.Providers.QBit;

internal partial class QBitContentManager
{

    private readonly ConcurrentDictionary<string, bool> _cleanupTorrents = [];

    private void EnqueueForCleanup(QBitTorrent torrent)
    {
        if (!_cleanupTorrents.TryAdd(torrent.Id, true)) return;

        BackgroundJob.Enqueue(() => CleanupTorrent(torrent.Id, CancellationToken.None));
    }

    [AutomaticRetry(Attempts = 0)] // Do not retry this we should be handling all meaningful errors
    [Queue(HangfireQueue.TorrentCleanup)]
    [DisableConcurrentExecution(timeoutInSeconds: 86400 * 2)] // 2 days
    public async Task CleanupTorrent(string hash, CancellationToken ct)
    {
        var torrent = await GetTorrent(hash, ct);
        if (torrent == null)
        {
            await cache.RemoveAsync(RequestCacheKey + hash, ct);
            _cleanupTorrents.TryRemove(hash, out _);

            return;
        }

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
        var connectionService = scope.ServiceProvider.GetRequiredService<IConnectionService>();

        var sw = Stopwatch.StartNew();

        try
        {
            var cleanupService = scope.ServiceProvider.GetRequiredService<ICleanupService>();
            await cleanupService.CleanupAsync(torrent, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cleanup torrent {TorrentId}", torrent.Id);
        }
        finally
        {
            await messageService.DeleteContent(torrent.Request.UserId, torrent.Id);
            connectionService.CommunicateDownloadFinished(torrent.DownloadInfo);

            _cleanupTorrents.TryRemove(torrent.Id, out _);

            unitOfWork.ImportedReleaseRepository.AddRange([
                new ContentRelease
                {
                    ReleaseId = torrent.Id,
                    ReleaseName = torrent.Title,
                    ContentName = torrent.Title,
                    Type = ReleaseType.Imported,
                    ReleaseDate = DateTime.UtcNow,
                    Provider = torrent.Request.Provider,
                }
            ]);

            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("[{Title}/{Id}] Cleaned up in {Elapsed}ms",  torrent.Title, torrent.Id, sw.ElapsedMilliseconds);
        }
    }

    private async Task<QBitTorrent?> GetTorrent(string hash, CancellationToken ct)
    {
        var request = await cache.GetAsJsonAsync<DownloadRequestDto>(RequestCacheKey + hash, token: ct);
        if (request == null)
        {
            logger.LogWarning("Tried to get a torrent without matching request: {Id}", hash);
            return null;
        }

        var query = new TorrentListQuery { Category = MnemaCategory, Hashes = [hash] };
        var qbitTorrents = await qBitClient.GetTorrentsAsync(query, ct);
        var qbitTorrent = qbitTorrents.FirstOrDefault(t => t.Hash == hash);
        if (qbitTorrent == null)
        {
            logger.LogWarning("Torrent to get no longer exists on the download client: {Id}", hash);
            return null;
        }

        return new QBitTorrent(request, qbitTorrent);
    }

}
