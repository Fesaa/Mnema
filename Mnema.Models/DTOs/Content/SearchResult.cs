using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record SearchResult
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required Provider Provider { get; set; }

    public string? Description { get; set; }
    public string? Size { get; set; }
    public IList<string> Tags { get; set; } = [];
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
}