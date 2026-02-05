using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface IDownloadClientRepository: IEntityRepository<DownloadClient, DownloadClientDto>
{
    Task<List<DownloadClientType>> GetInUseTypesAsync(CancellationToken cancellationToken);
    Task<DownloadClient?> GetDownloadClientAsync(DownloadClientType type, CancellationToken cancellationToken);
}

public interface IDownloadClientService
{
    Task MarkAsFailed(Guid id, CancellationToken cancellationToken);
    Task ReleaseFailedLock(Guid id, CancellationToken cancellationToken);
    Task UpdateDownloadClientAsync(DownloadClientDto dto, CancellationToken cancellationToken);
    Task<FormDefinition?> GetFormDefinitionForType(DownloadClientType type, CancellationToken cancellationToken);
}
