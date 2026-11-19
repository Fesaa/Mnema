using System;

namespace Mnema.Models.DTOs.Content;

public sealed class ExternalDownloadFileDto
{
    public required string Path { get; init; }
    public required string FileName { get; init; }
    public required long FileSize { get; init; }
    public required bool Selected { get; init; }
    public string? VolumeMarker { get; init; }
    public string? ChapterMarker { get; init; }
    public Guid? MonitoredChapterId { get; init; }
}
