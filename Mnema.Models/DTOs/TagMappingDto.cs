namespace Mnema.Models.DTOs;

public sealed record TagMappingDto
{
    public required string OriginTag { get; set; }
    public required string DestinationTag { get; set; }
}
