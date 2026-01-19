using System;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using QBittorrent.Client;

namespace Mnema.Providers.QBit;

public class QBitTorrent(DownloadRequestDto request, TorrentInfo torrentInfo) : IContent
{
    public string Id => torrentInfo.Hash;
    public string Title => torrentInfo.Name;
    public string DownloadDir => torrentInfo.SavePath;

    /// <summary>
    /// Optional series metadata that can be set during the process
    /// </summary>
    public Series? Series { get; set; }

    public ContentState State => torrentInfo.State switch
    {
        TorrentState.Unknown => ContentState.Waiting,
        TorrentState.Error => ContentState.Cancel,
        TorrentState.PausedUpload => ContentState.Cleanup,
        TorrentState.PausedDownload => ContentState.Waiting,
        TorrentState.QueuedUpload => ContentState.Cleanup,
        TorrentState.QueuedDownload => ContentState.Queued,
        TorrentState.Uploading => ContentState.Cleanup,
        TorrentState.StalledUpload => ContentState.Cleanup,
        TorrentState.CheckingUpload => ContentState.Cleanup,
        TorrentState.CheckingDownload => ContentState.Loading,
        TorrentState.Downloading => ContentState.Downloading,
        TorrentState.StalledDownload => ContentState.Downloading,
        TorrentState.FetchingMetadata => ContentState.Loading,
        TorrentState.ForcedFetchingMetadata => ContentState.Loading,
        TorrentState.ForcedUpload => ContentState.Cleanup,
        TorrentState.ForcedDownload => ContentState.Downloading,
        TorrentState.MissingFiles => ContentState.Cancel,
        TorrentState.Allocating => ContentState.Loading,
        TorrentState.QueuedForChecking => ContentState.Queued,
        TorrentState.CheckingResumeData => ContentState.Loading,
        TorrentState.Moving => ContentState.Cleanup,
        _ => throw new ArgumentOutOfRangeException()
    };

    public DownloadRequestDto Request => request;

    public DownloadInfo DownloadInfo => new()
    {
        Provider = request.Provider,
        Id = Id,
        ContentState = State,
        Name = Title,
        Description = Series?.Summary,
        ImageUrl = Series?.CoverUrl,
        RefUrl = Series?.RefUrl,
        Size = torrentInfo.Size.AsHumanReadableSize(),
        TotalSize = torrentInfo.TotalSize?.AsHumanReadableSize() ?? string.Empty,
        Downloading = State == ContentState.Downloading,
        Progress = Math.Floor(torrentInfo.Progress * 100),
        Estimated = State == ContentState.Downloading ? torrentInfo.EstimatedTime?.TotalSeconds ?? 0 : 0,
        SpeedType = SpeedType.Bytes,
        Speed = torrentInfo.DownloadSpeed,
        DownloadDir = Request.BaseDir,
        UserId = Request.UserId,
    };
}
