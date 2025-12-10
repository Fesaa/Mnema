namespace Mnema.API.Database;

public interface IUnitOfWork
{

    ISubscriptionRepository SubscriptionRepository { get; }
    IPagesRepository PagesRepository { get; }
    IUserRepository UserRepository { get; }
    
    Task<bool> CommitAsync();
    bool HasChanges();
    Task<bool> RollbackAsync();
}