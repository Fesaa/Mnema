using System.Text.Json.Serialization;

namespace Mnema.Models.DTOs.IO;

public class ListDirRequestDto
{
    [JsonPropertyName("dir")] public required string Directory { get; set; }

    [JsonPropertyName("files")] public required bool ShowFiles { get; set; }
}

public sealed record ListDirEntryDto(string Name, bool Dir);

public sealed record CreateDirRequestDto(string BaseDir, string NewDir);