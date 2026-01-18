using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using QBittorrent.Client;

namespace Mnema.Providers.QBit;

internal partial class QBitContentManager: IAsyncDisposable
{

    private static readonly IReadOnlyList<TorrentState> UploadStates = [
        TorrentState.Uploading, TorrentState.ForcedUpload, TorrentState.StalledUpload,
        TorrentState.PausedUpload, TorrentState.QueuedUpload,
    ];

    private readonly CancellationTokenSource _tokenSource = new();
    private Task? _watcherTask;

    private void EnsureWatcherInitialized()
    {
        if (_watcherTask != null)
            return;

        _watcherTask = Task.Run(() => _tokenSource.DoWhile(
            logger,
            TimeSpan.FromSeconds(2),
            TorrentWatcher,
            TorrentWatcherExceptionCatcher));
    }

    private async Task<bool> TorrentWatcherExceptionCatcher(Exception ex)
    {
        using var scope = scopeFactory.CreateScope();
        var downloadClientService = scope.ServiceProvider.GetRequiredService<IDownloadClientService>();

        switch (ex)
        {
            case HttpRequestException:
                if (_downloadClient != null)
                {
                    await downloadClientService.MarkAsFailed(_downloadClient.Id, _tokenSource.Token);
                }

                await ReloadConfiguration(CancellationToken.None);
                return false;

            default:
                return false;
        }
    }

    private async Task TorrentWatcher()
    {
        var client = await GetQBittorrentClient();
        if (client == null) return;

        var listQuery = new TorrentListQuery { Category = MnemaCategory };
        var torrents = await client.GetTorrentListAsync(listQuery);

        if (torrents == null || torrents.Count == 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            return;
        }

        using var scope = scopeFactory.CreateScope();

        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<QBitTorrent> inUploadState = [];
        List<QBitTorrent> queuedForSignalR = [];

        foreach (var tInfo in torrents)
        {
            var request = await cache.GetAsJsonAsync<DownloadRequestDto>(RequestCacheKey + tInfo.Hash);
            if (request == null) continue;

            (UploadStates.Contains(tInfo.State) ? inUploadState : queuedForSignalR).Add(new QBitTorrent(request, tInfo));
        }

        var uploadHashes = inUploadState.Select(t => t.Id).ToList();
        var nonImportedUploads = await unitOfWork.ImportedReleaseRepository.FilterReleases(uploadHashes);
        if (nonImportedUploads.Count == 0)
        {
            await UpdateUi(messageService, queuedForSignalR);
            return;
        }

        var dict = inUploadState.ToDictionary(t => t.Id);

        foreach (var id in nonImportedUploads)
        {
            if (!dict.TryGetValue(id, out var torrent))
                continue;

            CleanupTorrent(torrent);

            queuedForSignalR.Add(torrent);
        }

        await UpdateUi(messageService, queuedForSignalR);
    }

    private static async Task UpdateUi(IMessageService messageService, List<QBitTorrent> torrents)
    {
        var groups = torrents.GroupBy(t => t.Request.UserId);

        foreach (var group in groups)
        {
            await messageService.BulkContentInfoUpdate(group.Key, group.Select(t => t.DownloadInfo).ToArray());
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
