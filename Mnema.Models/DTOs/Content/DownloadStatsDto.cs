using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record DownloadInfo
{
    public required Provider Provider { get; init; }
    public required string Id { get; init; }
    public required ContentState ContentState { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required string? ImageUrl { get; init; }
    public required string? RefUrl { get; init; }
    public required string Size { get; init; }
    public required bool Downloading { get; init; }
    public required double Progress { get; init; }
    public required double Estimated { get; init; }
    public required SpeedType SpeedType { get; init; }
    public required double Speed { get; init; }
    public required string DownloadDir { get; init; }
    
}

public enum SpeedType
{
    Bytes = 0,
    Volumes = 1,
    Images = 2,
}