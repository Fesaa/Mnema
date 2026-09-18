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

    public ContentState State => externalDownload.State;

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
            var selectedFileCount = externalDownload.Files.Count(f => f.Selected);

            var totalSize = externalDownload.TotalFileSize.AsHumanReadableSize();
            var downloadedSize = externalDownload.SelectedFileSize.AsHumanReadableSize();

            double progress;
            if (State != ContentState.Cleanup)
            {
                progress = Math.Floor(torrentInfo.Progress * 100);
            }
            else
            {
                var processedFileCount = externalDownload.Files.Count(f => f is { Selected: true, Processed: true });
                progress = Math.Floor((double)processedFileCount / selectedFileCount * 100);
            }

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
                Size = $"{downloadedSize} {ToFileSuffix(selectedFileCount)}",
                TotalSize = $"{totalSize} {ToFileSuffix(externalDownload.Files.Count)}",
                Downloading = State == ContentState.Downloading,
                Progress = progress,
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
