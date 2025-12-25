using AutoMapper;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.UI;
using Mnema.Models.Entities.User;

namespace Mnema.Models;

public class AutoMapperProfiles: Profile
{

    public AutoMapperProfiles()
    {
        CreateMap<Subscription, SubscriptionDto>();
        CreateMap<Page, PageDto>();
        CreateMap<MnemaUser, UserDto>();
        CreateMap<Notification, NotificationDto>();
        CreateMap<UserPreferences, UserPreferencesDto>();
    }
    
}