using System.Collections.Generic;
using Mnema.Models.Entities.User;

namespace Mnema.Models.DTOs.User;

public class UserPreferencesDto
{
    public required ImageFormat ImageFormat { get; set; }
    public required CoverFallbackMethod CoverFallbackMethod { get; set; }
    public required IList<string> ConvertToGenreList { get; set; }
    public required IList<string> BlackListedTags { get; set; }
    public required IList<string> WhiteListedTags { get; set; }
    public required IList<AgeRatingMappingDto> AgeRatingMappings { get; set; }
    public required IList<TagMappingDto> TagMappings { get; set; }
    public bool PinSubscriptionTitles { get; set; }
}