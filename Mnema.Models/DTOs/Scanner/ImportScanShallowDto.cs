using System;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Enums;

namespace Mnema.Models.DTOs.Scanner;

public class ImportScanShallowDto : IDatabaseEntity
{
    public Guid Id { get; set; }
    public required string RootDir { get; set; }
    public required ImportScanStatus Status { get; set; }

    public int DirectoryImportResultCount { get; set; }
    public int ImportErrorCount { get; set; }

    public DateTime StartedUtc { get; set; }
    public DateTime? FinishedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
