using System.Threading.Tasks;
using Mnema.API.External;

namespace Mnema.API;

public interface IUnitOfWork
{

    ISubscriptionRepository SubscriptionRepository { get; }
    IPagesRepository PagesRepository { get; }
    IUserRepository UserRepository { get; }
    ISettingsRepository SettingsRepository { get; }
    INotificationRepository NotificationRepository { get; }
    IExternalConnectionRepository ExternalConnectionRepository { get; }
    
    Task<bool> CommitAsync();
    bool HasChanges();
    Task<bool> RollbackAsync();
}