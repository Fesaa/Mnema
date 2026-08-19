using System;
using System.IO;
using System.Linq;
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

    public ContentState State
    {
        get
        {
            var state = torrentInfo.State;
            var progress = torrentInfo.Progress;

            return state switch
            {
                TorrentState.Unknown => ContentState.Waiting,
                TorrentState.Error => ContentState.Cancel,

                TorrentState.PausedUpload
                    or TorrentState.QueuedUpload
                    or TorrentState.Uploading
                    or TorrentState.StalledUpload
                    or TorrentState.CheckingUpload
                    or TorrentState.ForcedUpload
                    or TorrentState.Moving
                    => progress >= 100 ? ContentState.Cleanup :
                progress == 0 ? ContentState.Waiting : ContentState.Downloading,

                TorrentState.PausedDownload => ContentState.Waiting,
                TorrentState.QueuedDownload => ContentState.Queued,
                TorrentState.CheckingDownload => ContentState.Loading,
                TorrentState.Downloading => ContentState.Downloading,
                TorrentState.StalledDownload => ContentState.Downloading,
                TorrentState.FetchingMetadata => ContentState.Loading,
                TorrentState.ForcedFetchingMetadata => ContentState.Loading,
                TorrentState.ForcedDownload => ContentState.Downloading,
                TorrentState.MissingFiles => ContentState.Cancel,
                TorrentState.Allocating => ContentState.Loading,
                TorrentState.QueuedForChecking => ContentState.Queued,
                TorrentState.CheckingResumeData => ContentState.Loading,

                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public DownloadRequestDto Request => new()
    {
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
            var totalSize = externalDownload.TotalFileSize.AsHumanReadableSize();
            var downloadedSize = externalDownload.SelectedFileSize.AsHumanReadableSize();

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
                Size = $"{downloadedSize} {ToFileSuffix(externalDownload.Files.Count(f => f.Selected))}",
                TotalSize = $"{totalSize} {ToFileSuffix(externalDownload.Files.Count)}",
                Downloading = State == ContentState.Downloading,
                Progress = Math.Floor(torrentInfo.Progress * 100),
                Estimated = State == ContentState.Downloading ? torrentInfo.EstimatedTime?.TotalSeconds ?? 0 : 0,
                SpeedType = SpeedType.Bytes,
                Speed = torrentInfo.DownloadSpeed,
                DownloadDir = Series != null ? Path.Join(Request.BaseDir, Title) : Request.BaseDir,
                MonitoredSeriesId = externalDownload.GetKey(RequestConstants.MonitoredSeriesId)
            };

            string ToFileSuffix(int count) => count == 1 ? $"({count} File)" : $"({count} Files)";
        }
    }
}
