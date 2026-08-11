using System;
using System.Collections.Generic;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Enums;

namespace Mnema.Models.Entities.Scanner;

public class DirectoryImportResult: IDatabaseEntity, IEntityDate
{
    public Guid Id { get; set; }

    public Guid ImportScanId { get; set; }
    public ImportScan ImportScan { get; set; }

    public required string Directory { get; set; }
    public required DirectoryImportStatus Status { get; set; }
    public int QueuePosition { get; set; }

    public Guid? MonitoredSeriesId { get; set; }
    public MonitoredSeries? MonitoredSeries { get; set; }

    public required string ParsedSeriesName { get; set; }
    public required int ParsedHardcoverId { get; set; }
    public required int ParsedMangaBakaId { get; set; }
    public required List<string> Files { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
