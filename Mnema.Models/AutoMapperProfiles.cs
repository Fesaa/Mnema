using AutoMapper;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Models;

public class AutoMapperProfiles: Profile
{

    public AutoMapperProfiles()
    {
        CreateMap<Subscription, SubscriptionDto>();
    }
    
}