using System;
using System.Collections.Generic;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Enums;

namespace Mnema.Models.DTOs.Scanner;

public class DirectoryImportResultDto: IDatabaseEntity
{
    public Guid Id { get; set; }

    public required string Directory { get; set; }
    public required DirectoryImportStatus Status { get; set; }

    public Guid ImportScanId { get; set; }
    public Guid? MonitoredSeriesId { get; set; }

    public required string ParsedSeriesName { get; set; }
    public required int ParsedHardcoverId { get; set; }
    public required int ParsedMangaBakaId { get; set; }
    public required List<string> Files { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
