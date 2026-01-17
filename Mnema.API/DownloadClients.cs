using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface IDownloadClientRepository
{
    Task<PagedList<DownloadClientDto>> GetAllDownloadClientsAsync(PaginationParams paginationParams, CancellationToken cancellationToken);
    Task<List<DownloadClientType>> GetInUseTypesAsync(CancellationToken cancellationToken);
    Task<DownloadClient?> GetDownloadClientAsync(Guid id, CancellationToken cancellationToken);
    Task<DownloadClient?> GetDownloadClientAsync(DownloadClientType type, CancellationToken cancellationToken);
    Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(DownloadClient downloadClient);
    void Update(DownloadClient downloadClient);
}

public interface IDownloadClientService
{
    Task UpdateDownloadClientAsync(DownloadClientDto dto, CancellationToken cancellationToken);
    Task<FormDefinition?> GetFormDefinitionForType(DownloadClientType type, CancellationToken cancellationToken);
}
