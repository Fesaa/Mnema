using System;
using System.Linq;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using QBittorrent.Client;

namespace Mnema.Providers.Managers.QBit;

public class ExternalDownloadContent(ExternalDownload externalDownload, TorrentInfo torrentInfo) : IContent
{
    public string Id => torrentInfo.Hash;
    public string Title => externalDownload.Title;
    public string DownloadDir => torrentInfo.SavePath;

    public TorrentInfo TorrentInfo => torrentInfo;

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

    public DownloadRequestDto Request => new()
    {
        UserId = externalDownload.UserId,
        Provider = externalDownload.Provider,
        Id = externalDownload.ExternalId,
        Metadata = externalDownload.Metadata,
        BaseDir = externalDownload.BaseDir,

        TempTitle = string.Empty,
    };

    public DownloadInfo DownloadInfo
    {
        get
        {
            var totalSize = externalDownload.Files.Select(f => f.FileSize).Sum().AsHumanReadableSize();
            var downloadedSize = externalDownload.Files
                .Where(f => f.Selected)
                .Select(f => f.FileSize)
                .Sum()
                .AsHumanReadableSize();

            return new DownloadInfo
            {
                Provider = externalDownload.Provider,
                Id = externalDownload.Id.ToString(),
                ContentState = State,
                Name = Title,
                Description = Series?.Summary,
                ImageUrl = Series?.CoverUrl,
                RefUrl = Series?.RefUrl,
                ReDownloadSize = string.Empty,
                Size = downloadedSize,
                TotalSize = totalSize,
                Downloading = State == ContentState.Downloading,
                Progress = Math.Floor(torrentInfo.Progress * 100),
                Estimated = State == ContentState.Downloading ? torrentInfo.EstimatedTime?.TotalSeconds ?? 0 : 0,
                SpeedType = SpeedType.Bytes,
                Speed = torrentInfo.DownloadSpeed,
                DownloadDir = externalDownload.BaseDir,
                UserId = externalDownload.UserId,
                MonitoredSeriesId = externalDownload.GetKey(RequestConstants.MonitoredSeriesId)
            };
        }
    }
}
