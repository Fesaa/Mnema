using System;

namespace Mnema.Models.DTOs.Content;

public sealed class UpdateDownloadFileMarkersDto
{
    public required string Path { get; init; }
    public Guid? MonitoredChapterId { get; init; }
    public string? VolumeMarker { get; init; }
    public string? ChapterMarker { get; init; }
}
