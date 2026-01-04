using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.External;

namespace Mnema.API.External;

public interface IExternalConnectionService
{
    void CommunicateDownloadStarted(DownloadInfo info);
    void CommunicateDownloadFinished(DownloadInfo info);
    void CommunicateDownloadFailure(DownloadInfo info, Exception ex);
    
    Task UpdateConnection(ExternalConnectionDto connection, CancellationToken cancellationToken);
    Task<FormDefinition> GetForm(ExternalConnectionType type, CancellationToken cancellationToken);
}

public interface IExternalConnectionHandlerService
{
    List<ExternalConnectionEvent> SupportedEvents { get; }

    Task CommunicateDownloadStarted(ExternalConnection connection, DownloadInfo info);
    Task CommunicateDownloadFinished(ExternalConnection connection, DownloadInfo info);
    Task CommunicateDownloadFailure(ExternalConnection connection, DownloadInfo info, Exception ex);
    
    /// <summary>
    /// Returns the form for configuration this specific external service
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>Throws <see cref="NotImplementedException"/> when called on a non-keyed implementation</remarks>
    Task<List<FormControlDefinition>> GetConfigurationFormControls(CancellationToken cancellationToken);
}

public interface IExternalConnectionRepository
{
    Task<List<ExternalConnection>> GetAllConnections(CancellationToken cancellationToken);
    Task<PagedList<ExternalConnectionDto>> GetAllConnectionDtos(PaginationParams pagintation, CancellationToken cancellationToken);
    Task<ExternalConnection?> GetConnectionById(Guid connectionId, CancellationToken cancellationToken);
    Task DeleteConnectionById(Guid connectionId, CancellationToken cancellationToken);
    
    
    void Add(ExternalConnection connection);
    void Update(ExternalConnection connection);
}