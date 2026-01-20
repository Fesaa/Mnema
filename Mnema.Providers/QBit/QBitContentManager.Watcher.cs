using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using QBittorrent.Client;

namespace Mnema.Providers.QBit;

internal partial class QBitContentManager
{

    private static readonly IReadOnlyList<TorrentState> UploadStates = [
        TorrentState.Uploading, TorrentState.ForcedUpload, TorrentState.StalledUpload,
        TorrentState.PausedUpload, TorrentState.QueuedUpload,
    ];

    public async Task TorrentWatcher()
    {
        IReadOnlyList<TorrentInfo> torrents;
        try
        {
            var listQuery = new TorrentListQuery { Category = MnemaCategory };
            torrents = await qBitClient.GetTorrentsAsync(listQuery);
        }
        catch (Exception ex) when (ex is HttpRequestException or QBittorrentClientRequestException or MnemaException)
        {
            return;
        }

        if (torrents.Count == 0)
        {
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

            EnqueueForCleanup(torrent);

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
}
