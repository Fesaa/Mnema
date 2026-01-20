using System.Threading;
using System.Threading.Tasks;

namespace Mnema.API;

public interface IUnitOfWork
{
    IPagesRepository PagesRepository { get; }
    IUserRepository UserRepository { get; }
    ISettingsRepository SettingsRepository { get; }
    INotificationRepository NotificationRepository { get; }
    IConnectionRepository ConnectionRepository { get; }
    IContentReleaseRepository ContentReleaseRepository { get; }
    IDownloadClientRepository DownloadClientRepository { get; }
    IContentReleaseRepository ImportedReleaseRepository { get; }
    IMonitoredSeriesRepository MonitoredSeriesRepository { get; }

    Task<bool> CommitAsync(CancellationToken cancellationToken = default);
    bool HasChanges();
    Task<bool> RollbackAsync(CancellationToken cancellationToken = default);
}
