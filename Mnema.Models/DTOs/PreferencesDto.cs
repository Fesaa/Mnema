using System.Collections.Generic;
using Mnema.Models.Entities.User;

namespace Mnema.Models.DTOs;

public class PreferencesDto
{
    public required ImageFormat ImageFormat { get; set; }
    public required CoverFallbackMethod CoverFallbackMethod { get; set; }
    public required IList<string> BlackListedTags { get; set; }
    public required IList<string> WhiteListedTags { get; set; }
    public required IList<AgeRatingMappingDto> AgeRatingMappings { get; set; }
    public required IList<MetadataFieldMappingDto> MetadataFieldMappings { get; set; }
    public required bool PinSubscriptionTitles { get; set; }
    public required string ChapterFileFormat { get; set; }
    public required string OneShotFileFormat { get; set; }
}
