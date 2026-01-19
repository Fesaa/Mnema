using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface ISubscriptionRepository
{
    Task<PagedList<SubscriptionDto>> GetSubscriptionDtosForUser(Guid userId, string query, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<Subscription?> GetSubscription(Guid id, CancellationToken cancellationToken = default);
    Task<Subscription?> GetSubscriptionByContentId(string contentId, CancellationToken cancellationToken = default);
    Task<SubscriptionDto?> GetSubscriptionDto(Guid id, CancellationToken cancellationToken = default);
    Task<List<Subscription>> GetAllSubscriptions(CancellationToken cancellationToken = default);

    void Update(Subscription subscription);
    void Add(Subscription subscription);
    void Delete(Subscription subscription);
}

public interface ISubscriptionService
{
    public static readonly ImmutableArray<Provider> SubscriptionProviders =
    [
        Provider.Bato, Provider.Dynasty,
        Provider.Mangadex, Provider.Webtoons
    ];

    public Task UpdateSubscription(Guid userId, CreateOrUpdateSubscriptionDto dto, CancellationToken cancellationToken = default);
    public Task CreateSubscription(Guid userId, CreateOrUpdateSubscriptionDto dto, CancellationToken cancellationToken = default);
    public Task RunOnce(Guid userId, Guid subId);
    public FormDefinition GetForm();
}
