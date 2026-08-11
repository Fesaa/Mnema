using System.Threading;
using System.Threading.Tasks;
using Mnema.API.Content;
using Mnema.API.Repositories;

namespace Mnema.API;

public interface IUnitOfWork
{
    IPagesRepository PagesRepository { get; }
    ISettingsRepository SettingsRepository { get; }
    INotificationRepository NotificationRepository { get; }
    IConnectionRepository ConnectionRepository { get; }
    IContentReleaseRepository ContentReleaseRepository { get; }
    IDownloadClientRepository DownloadClientRepository { get; }
    IContentReleaseRepository ImportedReleaseRepository { get; }
    IMonitoredSeriesRepository MonitoredSeriesRepository { get; }
    IAuthKeyRepository AuthKeyRepository { get; }
    IProviderSettingsRepository ProviderSettingsRepository { get; }
    IExternalDownloadRepository ExternalDownloadRepository { get; }
    IMetadataProviderSettingsRepository MetadataProviderSettingsRepository { get; }
    IImportScanRepository ImportScanRepository { get; }

    Task<bool> CommitAsync(CancellationToken cancellationToken = default);
    bool HasChanges();
    Task<bool> RollbackAsync(CancellationToken cancellationToken = default);
}
