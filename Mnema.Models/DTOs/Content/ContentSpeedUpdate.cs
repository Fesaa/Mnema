namespace Mnema.Models.DTOs.Content;

public record ContentUpdate
{
    public required string ContentId { get; set; }
}

public sealed record ContentSpeedUpdate: ContentUpdate
{
    public required int Progress { get; set; }
    public required SpeedType SpeedType { get; set; }
    public required int Speed { get; set; }
    public int Estimated { get; set; }
}

public sealed record ContentSizeUpdate : ContentUpdate
{
    public required string Size { get; set; }
}

public sealed record ContentStateUpdate: ContentUpdate
{
    public required ContentState ContentState { get; set; }
}