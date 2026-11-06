namespace Mnema.Models.DTOs.Content;

public sealed record CoverDto
{
    public required string Url { get; init; }
    public required string Extension { get; init; }
    public string Volume { get; init; } = string.Empty;
    public string Chapter { get; init; } = string.Empty;
}
