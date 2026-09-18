using System;
using System.Collections.Generic;
using System.Linq;
using Mnema.API.Repositories;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.Entities.Content;

public class DroppedContent: IDatabaseEntity
{
    public Guid Id { get; set; }

    public Guid MonitoredSeriesId { get; set; }
    public MonitoredSeries MonitoredSeries { get; set; }

    [JsonColumn]
    public List<DownloadFile> Files { get; set; }

    public long TotalFileSize => Files
        .Select(f => f.FileSize)
        .Sum();

}
