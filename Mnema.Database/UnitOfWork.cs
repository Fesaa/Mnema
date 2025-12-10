using AutoMapper;
using Microsoft.Extensions.Logging;
using Mnema.API.Database;
using Mnema.Database.Repositories;

namespace Mnema.Database;

public class UnitOfWork(ILogger<UnitOfWork> logger, MnemaDataContext ctx, IMapper mapper): IUnitOfWork
{

    public ISubscriptionRepository SubscriptionRepository { get; } = new SubscriptionRepository(ctx, mapper);
    public IPagesRepository PagesRepository { get; } = new PagesRepository(ctx, mapper);
    public IUserRepository UserRepository { get; } = new UserRepository(ctx, mapper);
    
    public async Task<bool> CommitAsync()
    {
        return await ctx.SaveChangesAsync() > 0;
    }

    public bool HasChanges()
    {
        return ctx.ChangeTracker.HasChanges();
    }

    public async Task<bool> RollbackAsync()
    {
        try
        {
            await ctx.Database.RollbackTransactionAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occured during rollback");
        }

        return true;
    }
    
}