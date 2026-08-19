using System;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Enums;

namespace Mnema.Models.DTOs.Scanner;

public class ImportErrorDto: IDatabaseEntity
{
    public Guid Id { get; set; }

    public required ImportErrorType Type { get; set; }
    public required string Path { get; set; }
    public required string Message { get; set; }
    public string? StackTrace { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
