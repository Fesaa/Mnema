using Mnema.Models.Enums;

namespace Mnema.Models.DTOs.Content;

public sealed record ParsedSeriesUrlDto
{
    public Provider? Provider { get; set; }
    public MetadataProvider? MetadataProvider { get; set; }
    public required string ExternalId { get; set; }
    public required string SeriesName { get; set; }
    public string? Description { get; set; }
}
