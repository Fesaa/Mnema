using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

public class SubscriptionRepository(MnemaDataContext ctx, IMapper mapper) : ISubscriptionRepository
{
    public Task<PagedList<SubscriptionDto>> GetSubscriptionDtosForUser(Guid userId, string query,
        PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var queryMatcher = $"%{query.ToLower()}%";

        return ctx.Subscriptions
            .Where(s => s.UserId == userId && EF.Functions.Like(s.Title.ToLower(), queryMatcher))
            .ProjectTo<SubscriptionDto>(mapper.ConfigurationProvider)
            .OrderBy(s => s.Id)
            .AsPagedList(pagination, cancellationToken: cancellationToken);
    }

    public Task<Subscription?> GetSubscription(Guid id, CancellationToken cancellationToken = default)
    {
        return ctx.Subscriptions
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    public Task<Subscription?> GetSubscriptionByContentId(string contentId, CancellationToken cancellationToken = default)
    {
        return ctx.Subscriptions
            .Where(s => s.ContentId == contentId)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    public async Task<SubscriptionDto?> GetSubscriptionDto(Guid id, CancellationToken cancellationToken = default)
    {
        var sub = await ctx.Subscriptions
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return sub == null ? null : mapper.Map<SubscriptionDto>(sub);
    }

    public Task<List<Subscription>> GetAllSubscriptions(CancellationToken cancellationToken = default)
    {
        return ctx.Subscriptions.ToListAsync(cancellationToken: cancellationToken);
    }

    public void Update(Subscription subscription)
    {
        ctx.Subscriptions.Add(subscription).State = EntityState.Modified;
    }

    public void Add(Subscription subscription)
    {
        ctx.Subscriptions.Add(subscription).State = EntityState.Added;
    }

    public void Delete(Subscription subscription)
    {
        ctx.Subscriptions.Remove(subscription).State = EntityState.Deleted;
    }
}
