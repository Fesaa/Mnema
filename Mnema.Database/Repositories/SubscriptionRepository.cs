using AutoMapper;

namespace Mnema.Database.Repositories;

public interface ISubscriptionRepository
{
    
}

public class SubscriptionRepository(MnemaDataContext ctx, IMapper mapper): ISubscriptionRepository
{
    
}