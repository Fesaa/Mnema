using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface ISubscriptionScheduler
{
    Task EnsureScheduledAsync();
    Task RescheduleAsync(int hour);
}

public interface ISubscriptionRepository
{
    Task<PagedList<SubscriptionDto>> GetSubscriptionDtosForUser(Guid userId, string query, PaginationParams pagination);
    Task<Subscription?> GetSubscription(Guid id);
    Task<SubscriptionDto?> GetSubscriptionDto(Guid id);
    Task<List<Subscription>> GetAllSubscriptions();

    void Update(Subscription subscription);
    void Add(Subscription subscription);
    void Delete(Subscription subscription);
}

public interface ISubscriptionService
{
    public static readonly ImmutableArray<Provider> SubscriptionProviders =
    [
        Provider.Bato, Provider.Dynasty, Provider.MangaBuddy,
        Provider.Mangadex, Provider.Webtoons
    ];

    public Task UpdateSubscription(Guid userId, SubscriptionDto dto);
    public Task CreateSubscription(Guid userId, SubscriptionDto dto);
    public Task RunOnce(Guid userId, Guid subId);
}