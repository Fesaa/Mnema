using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using QBittorrent.Client;

namespace Mnema.Providers.Managers.QBit;

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

        var downloads = await unitOfWork.ExternalDownloadRepository.GetByExternalIds(torrents.Select(t => t.Hash));

        List<ExternalDownloadContent> inUploadState = [];
        List<ExternalDownloadContent> queuedForSignalR = [];

        foreach (var tInfo in torrents)
        {
            if (downloads.TryGetValue(tInfo.Hash, out var externalDownloads))
            {
                foreach (var externalDownload in externalDownloads)
                {
                    (UploadStates.Contains(tInfo.State) ? inUploadState : queuedForSignalR).Add(new ExternalDownloadContent(externalDownload, tInfo));
                }
            }
        }

        var uploadHashes = inUploadState.Select(t => t.Id).ToList();
        var nonImportedUploads = await unitOfWork.ImportedReleaseRepository.FilterReleases(uploadHashes);
        if (nonImportedUploads.Count == 0)
        {
            await UpdateUi(messageService, queuedForSignalR);
            return;
        }

        foreach (var id in nonImportedUploads)
        {
            if (!_cleanupTorrents.TryAdd(id, true)) return;

            BackgroundJob.Enqueue(() => CleanupTorrent(id, CancellationToken.None));

            queuedForSignalR.AddRange(inUploadState.Where(t => t.Id == id));
        }

        await UpdateUi(messageService, queuedForSignalR);
    }

    private static async Task UpdateUi(IMessageService messageService, List<ExternalDownloadContent> torrents)
    {
        var groups = torrents.GroupBy(t => t.Request.UserId);

        foreach (var group in groups)
        {
            await messageService.BulkContentInfoUpdate(group.Key, group.Select(t => t.DownloadInfo).ToArray());
        }
    }
}
