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

        var downloads = await unitOfWork.ExternalDownloadRepository
            .GetByExternalIds(torrents.Select(t => t.Hash));

        var content = torrents
            .Where(t => downloads.ContainsKey(t.Hash))
            .SelectMany(t => downloads[t.Hash].Select(ed => new ExternalDownloadContent(ed, t)))
            .ToList();

        var toProcessFinishedContentHashes = content
            .Where(c => UploadStates.Contains(c.TorrentInfo.State))
            .Where(c => !_cleanupTorrents.ContainsKey(c.Id))
            .Select(c => c.Id)
            .ToHashSet();

        await messageService.BulkContentInfoUpdate(content.Select(t => t.DownloadInfo).ToArray());

        foreach (var id in toProcessFinishedContentHashes.Where(id => _cleanupTorrents.TryAdd(id, true)))
            BackgroundJob.Enqueue(() => CleanupTorrent(id, CancellationToken.None));
    }
}
