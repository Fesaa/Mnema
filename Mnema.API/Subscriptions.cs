using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface ISubscriptionScheduler
{
    Task EnsureScheduledAsync();
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

    public Task UpdateSubscription(Guid userId, CreateOrUpdateSubscriptionDto dto);
    public Task CreateSubscription(Guid userId, CreateOrUpdateSubscriptionDto dto);
    public Task RunOnce(Guid userId, Guid subId);
    public FormDefinition GetForm();
}
