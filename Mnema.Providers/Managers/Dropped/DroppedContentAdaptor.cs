using System;
using System.Linq;
using Mnema.API;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;

namespace Mnema.Providers.Managers.Dropped;

public class DroppedContentAdaptor(DroppedContent content): IContent
{
    public string Id => content.Id.ToString();
    public string Title => content.MonitoredSeries.Title;
    public string DownloadDir => content.MonitoredSeriesId.ToString();
    /// Is always in cleanup, as the files are uploaded by the user
    public ContentState State => ContentState.Cleanup;

    public DownloadRequestDto Request
    {
        get
        {
            var metadata = content.MonitoredSeries.MetadataForDownloadRequest();
            metadata.SetKey(RequestConstants.DroppedContentId, content.Id);

            return new DownloadRequestDto
            {
                Provider = Provider.GenericFile,
                Id = Id,
                BaseDir = content.MonitoredSeries.BaseDir,
                TempTitle = content.MonitoredSeries.Title,
                Metadata = metadata,
            };
        }
    }

    public DownloadInfo DownloadInfo => new()
    {
        Provider = Provider.GenericFile,
        Id = Id,
        ContentState = State,
        Name = content.MonitoredSeries.Title,
        Description = content.MonitoredSeries.Summary,
        ImageUrl = content.MonitoredSeries.CoverUrl,
        RefUrl = null,
        Size = ToFileSuffix(content.Files.Count),
        ReDownloadSize = string.Empty,
        TotalSize = ToFileSuffix(content.Files.Count),
        Downloading = false,
        Progress = Math.Floor((double) content.Files.Count(f => f.Processed) / content.Files.Count * 100),
        Estimated = 0,
        SpeedType = SpeedType.Bytes,
        Speed = 0,
        DownloadDir = content.MonitoredSeries.BaseDir,
        MonitoredSeriesId = content.MonitoredSeries.Id,
    };

    private static string ToFileSuffix(int count) => count == 1 ? $"({count} File)" : $"({count} Files)";
}
