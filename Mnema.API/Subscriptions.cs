using System.Collections.Immutable;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface ISubscriptionRepository
{
    
}

public interface ISubscriptionService
{

    public static ImmutableArray<Provider> SubscriptionProviders = [
        Provider.Bato, Provider.Dynasty, Provider.MangaBuddy,
        Provider.Mangadex, Provider.Webtoons,
    ];

}