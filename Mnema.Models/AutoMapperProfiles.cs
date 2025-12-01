using AutoMapper;
using Mnema.Models.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Models;

public class AutoMapperProfiles: Profile
{

    public AutoMapperProfiles()
    {
        CreateMap<Subscription, SubscriptionDto>();
    }
    
}