using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;

namespace Mnema.API;

public interface IConnectionService
{
    void CommunicateDownloadStarted(DownloadInfo info);
    void CommunicateDownloadFinished(DownloadInfo info);
    void CommunicateDownloadFailure(DownloadInfo info, Exception ex);
    void CommunicateSubscriptionExhausted(DownloadInfo info);

    Task UpdateConnection(ConnectionDto connection, CancellationToken cancellationToken);
    Task<FormDefinition> GetForm(ConnectionType type, CancellationToken cancellationToken);
}

public interface IConnectionHandlerService
{
    List<ConnectionEvent> SupportedEvents { get; }

    Task CommunicateDownloadStarted(Connection connection, DownloadInfo info);
    Task CommunicateDownloadFinished(Connection connection, DownloadInfo info);
    Task CommunicateDownloadFailure(Connection connection, DownloadInfo info, Exception ex);
    Task CommunicateSubscriptionExhausted(Connection connection, DownloadInfo info);

    /// <summary>
    ///     Returns the form for configuration this specific external service
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>Throws <see cref="NotImplementedException" /> when called on a non-keyed implementation</remarks>
    Task<List<FormControlDefinition>> GetConfigurationFormControls(CancellationToken cancellationToken);
}

public interface IConnectionRepository: IEntityRepository<Connection, ConnectionDto>;
