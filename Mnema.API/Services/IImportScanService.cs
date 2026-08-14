using System;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Scanner;

namespace Mnema.API.Services;

public interface IImportScanService
{
    Task RejectDirectoryImport(Guid id, CancellationToken cancellationToken);
    Task SkipDirectoryImport(Guid id, CancellationToken cancellationToken);
    Task UpdateDirectoryImport(Guid id, UpdateDirectoryImportResultDto dto, CancellationToken cancellationToken);
    Task AutoAcceptDirectoryImport(Guid id, CancellationToken cancellationToken);
}
