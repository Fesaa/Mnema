using Mnema.Models.Publication;

namespace Mnema.Models.DTOs.User;

public sealed record AgeRatingMappingDto
{
    public required string Tag { get; set; }
    public required AgeRating AgeRating { get; set; }
}