using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using QBittorrent.Client;

namespace Mnema.Providers.QBit;

internal partial class QBitContentManager: IAsyncDisposable
{

    private readonly CancellationTokenSource _tokenSource = new();
    private Task? _watcherTask;

    private void EnsureWatcherInitialized()
    {
        if (_watcherTask != null)
            return;

        _watcherTask = Task.Run(() => _tokenSource.DoWhile(
            logger,
            TimeSpan.FromSeconds(2),
            TorrentWatcher));
    }

    private async Task TorrentWatcher()
    {
        var client = await GetQBittorrentClient();
        if (client == null) return;

        var listQuery = new TorrentListQuery() { Category = MnemaCategory };
        var torrents = await client.GetTorrentListAsync(listQuery);
        if (torrents == null) return;

        using var scope = scopeFactory.CreateScope();

        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

        foreach (var tInfo in torrents)
        {
            var request = await cache.GetAsJsonAsync<DownloadRequestDto>(RequestCacheKey + tInfo.Hash);
            if (request == null) continue;

            var content = new QBitTorrent(request, tInfo);

            if (tInfo.State is TorrentState.Uploading or TorrentState.StalledUpload or TorrentState.ForcedUpload)
            {
                CleanupTorrent(content);
            }
            else
            {
                await messageService.UpdateContent(request.UserId, content.DownloadInfo);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _tokenSource.CancelAsync();
        if (_watcherTask != null)
        {
            try
            {
                await _watcherTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _tokenSource.Dispose();

        _qBittorrentClient?.Dispose();
    }
}
