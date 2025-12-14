using Mnema.Models.DTOs.User;

namespace Mnema.Models.Entities.User;

public class UserPreferences
{
    
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public MnemaUser User { get; set; }

    public required ImageFormat ImageFormat { get; set; }
    public required CoverFallbackMethod CoverFallbackMethod { get; set; }
    public required IList<string> ConvertToGenreList { get; set; }
    public required IList<string> BlackListedTags { get; set; }
    public required IList<string> WhiteListedTags { get; set; }
    public required IList<AgeRatingMappingDto> AgeRatingMappings { get; set; }
    public required IList<TagMappingDto> TagMappings { get; set; }
    public bool PinSubscriptionTitles { get; set; }
    
    
}