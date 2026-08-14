using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.Scanner;
using Mnema.Models.Entities.Scanner;
using Mnema.Models.Enums;

namespace Mnema.API.Repositories;

[Flags]
public enum ImportScanIncludes
{
    None = 0,
    DirectoryImports = 1 << 0,
    ImportErrors = 1 << 1,
}

public interface IImportScanRepository : INavigationalEntityRepository<ImportScan, ImportScanDto, ImportScanIncludes>
{
    Task<PagedList<ImportScanShallowDto>> GetShallowScansPaged(PaginationParams paginationParams, CancellationToken cancellationToken);

    Task<PagedList<DirectoryImportResultDto>> GetDirectoryImportsPaged(Guid scanId, PaginationParams paginationParams, CancellationToken cancellationToken);
    Task<PagedList<ImportErrorDto>> GetImportErrorsPaged(Guid scanId, PaginationParams paginationParams, CancellationToken cancellationToken);

    Task<bool> HasNonFinishedScan(string root, CancellationToken cancellationToken);

    Task<HashSet<string>> GetAlreadyLinkedDirectoriesForRoot(string root, CancellationToken cancellationToken);

    Task<int> GetMaxQueuePosition(Guid scanId, DirectoryImportStatus status, CancellationToken cancellationToken);
    Task<DirectoryImportResult?> GetDirectoryImportResult(Guid id, CancellationToken cancellationToken);
    Task<ImportError?> GetImportError(Guid id, CancellationToken cancellationToken);

}
