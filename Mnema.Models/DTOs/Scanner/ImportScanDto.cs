using System;
using System.Collections.Generic;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Entities.Scanner;
using Mnema.Models.Enums;

namespace Mnema.Models.DTOs.Scanner;

public class ImportScanDto: IDatabaseEntity
{
    public Guid Id { get; set; }

    public required string RootDir { get; set; }
    public required ImportScanStatus Status { get; set; }

    public List<DirectoryImportResult> DirectoryImportResults { get; set; }
    public List<ImportError> ImportErrors { get; set; }

    public DateTime StartedUtc { get; set; }
    public DateTime? FinishedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
