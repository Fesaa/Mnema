using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

public class SubscriptionRepository(MnemaDataContext ctx, IMapper mapper): ISubscriptionRepository
{

    public Task<List<SubscriptionDto>> GetSubscriptionDtosForUser(Guid userId)
    {
        return ctx.Subscriptions
            .Where(s => s.UserId == userId)
            .ProjectTo<SubscriptionDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public Task<Subscription?> GetSubscription(Guid id)
    {
        return ctx.Subscriptions
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<SubscriptionDto?> GetSubscriptionDto(Guid id)
    {
        var sub = await ctx.Subscriptions
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();

        return sub == null ? null : mapper.Map<SubscriptionDto>(sub);
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