using System;
using System.Collections.Generic;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Enums;

namespace Mnema.Models.Entities.Scanner;

public class ImportError: IDatabaseEntity, IEntityDate
{
    public Guid Id { get; set; }

    public Guid ImportScanId { get; set; }
    public ImportScan ImportScan { get; set; }

    public required ImportErrorType Type { get; set; }
    public required string Path { get; set; }
    public required string Message { get; set; }
    public string? StackTrace { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    private ImportError() { }

    public static ImportError UnknownDirectory(string path) => new()
    {
        Path = path,
        Message = "Could not find directory with such path",
        Type = ImportErrorType.UnknownDirectory
    };

    public static ImportError FromException(string path, Exception ex) => new()
    {
        Path = path,
        Message = ex.Message,
        StackTrace = ex.StackTrace,
        Type = ImportErrorType.GenericException
    };

    public static ImportError MixedContentFormats(string path, List<string> extensions) => new()
    {
        Path = path,
        Message = $"Mixed content formats: {string.Join(", ", extensions)}",
        Type = ImportErrorType.MixedContentFormats
    };

    public static ImportError FailedToParseContentFormat(string path, string fileName) => new()
    {
        Path = path,
        Message = $"Failed to parse content format for file '{fileName}'",
        Type = ImportErrorType.FailedToParseContentFormat
    };

    public static ImportError FailedToParseSeries(string path, string fileName) => new()
    {
        Path = path,
        Message = $"Failed to parse series for file '{fileName}'",
        Type = ImportErrorType.FailedToParseSeries
    };
}
