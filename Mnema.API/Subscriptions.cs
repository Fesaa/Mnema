using System.Collections.Immutable;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface ISubscriptionRepository
{

    Task<PagedList<SubscriptionDto>> GetSubscriptionDtosForUser(Guid userId, string query, PaginationParams pagination);
    Task<Subscription?> GetSubscription(Guid id);
    Task<SubscriptionDto?> GetSubscriptionDto(Guid id);

    void Update(Subscription subscription);
    void Add(Subscription subscription);
    void Delete(Subscription subscription);

}

public interface ISubscriptionService
{

    public static ImmutableArray<Provider> SubscriptionProviders = [
        Provider.Bato, Provider.Dynasty, Provider.MangaBuddy,
        Provider.Mangadex, Provider.Webtoons,
    ];

    public Task UpdateSubscription(Guid userId, SubscriptionDto dto);
    public Task CreateSubscription(Guid userId, SubscriptionDto dto);

}