using System;
using System.Collections.Generic;
using Mnema.Models.DTOs;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Entities.User;

namespace Mnema.Models.Entities;

public class Preferences: IDatabaseEntity
{
    public Guid Id { get; set; }

    public required ImageFormat ImageFormat { get; set; }
    public required CoverFallbackMethod CoverFallbackMethod { get; set; }
    public required IList<string> BlackListedTags { get; set; }
    public required IList<string> WhiteListedTags { get; set; }
    public required IList<AgeRatingMappingDto> AgeRatingMappings { get; set; }
    public required IList<MetadataFieldMappingDto> MetadataFieldMappings { get; set; }
    [Obsolete("Use MetadataFieldMappings")]
    public required IList<string> ConvertToGenreList { get; set; }
    [Obsolete("Use MetadataFieldMappings")]
    public required IList<TagMappingDto> TagMappings { get; set; }
    public required bool PinSubscriptionTitles { get; set; }
    public required string ChapterFileFormat { get; set; }
    public required string OneShotFileFormat { get; set; }
}
