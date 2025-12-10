namespace Mnema.API.Database;

public interface IUnitOfWork
{

    ISubscriptionRepository SubscriptionRepository { get; }
    
    Task<bool> CommitAsync();
    bool HasChanges();
    Task<bool> RollbackAsync();
}