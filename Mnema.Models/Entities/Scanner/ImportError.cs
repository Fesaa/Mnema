using System;
using System.Collections.Generic;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.Entities.Scanner;

public class ImportError: IDatabaseEntity, IEntityDate
{
    public Guid Id { get; set; }

    public required string Path { get; set; }
    public required string Message { get; set; }
    public string? StackTrace { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    public static ImportError UnknownDirectory(string path) => new()
    {
        Path = path,
        Message = "Could not find directory with such path"
    };

    public static ImportError FromException(string path, Exception ex) => new()
    {
        Path = path,
        Message = ex.Message,
        StackTrace = ex.StackTrace
    };

    public static ImportError MixedContentFormats(string path, List<string> extensions) => new()
    {
        Path = path,
        Message = $"Mixed content formats: {string.Join(", ", extensions)}"
    };

    public static ImportError FailedToParseContentFormat(string path, string fileName) => new()
    {
        Path = path,
        Message = $"Failed to parse content format for file '{fileName}'"
    };

    public static ImportError FailedToParseSeries(string path, string fileName) => new()
    {
        Path = path,
        Message = $"Failed to parse series for file '{fileName}'"
    };
}
