using System;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API;
using Mnema.API.Services;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.Scanner;
using Mnema.Models.Enums;

namespace Mnema.Services;

public class ImportScanService(IUnitOfWork unitOfWork, IMonitoredSeriesService monitoredSeriesService): IImportScanService
{
    public async Task RejectDirectoryImport(Guid id, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.ImportScanRepository.GetDirectoryImportResult(id, cancellationToken);
        if (result == null)
        {
            throw new NotFoundException();
        }

        if (result.Status == DirectoryImportStatus.Imported)
        {
            throw new BadRequestException();
        }

        var max = await unitOfWork.ImportScanRepository
            .GetMaxQueuePosition(result.ImportScanId, DirectoryImportStatus.Rejected, cancellationToken);

        result.Status = DirectoryImportStatus.Rejected;
        result.QueuePosition = max + 1;
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task SkipDirectoryImport(Guid id, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.ImportScanRepository.GetDirectoryImportResult(id, cancellationToken);
        if (result == null)
        {
            throw new NotFoundException();
        }

        if (result.Status == DirectoryImportStatus.Imported)
        {
            throw new BadRequestException();
        }

        var max = await unitOfWork.ImportScanRepository
            .GetMaxQueuePosition(result.ImportScanId, DirectoryImportStatus.Queued, cancellationToken);

        result.QueuePosition = max + 1;
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task UpdateDirectoryImport(Guid id, UpdateDirectoryImportResultDto dto, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.ImportScanRepository.GetDirectoryImportResult(id, cancellationToken);
        if (result == null)
        {
            throw new NotFoundException();
        }

        if (result.Status == DirectoryImportStatus.Imported)
        {
            throw new BadRequestException();
        }

        if (string.IsNullOrEmpty(dto.ParsedSeriesName))
        {
            throw new BadRequestException("Parsed series name is required");
        }

        result.ParsedSeriesName = dto.ParsedSeriesName;
        result.ParsedHardcoverId = dto.ParsedHardcoverId;
        result.ParsedMangaBakaId = dto.ParsedMangaBakaId;

        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task AutoAcceptDirectoryImport(Guid id, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.ImportScanRepository.GetDirectoryImportResult(id, cancellationToken);
        if (result == null)
        {
            throw new NotFoundException();
        }

        var scan = await unitOfWork.ImportScanRepository.GetById(result.ImportScanId, ct: cancellationToken);
        if (scan == null)
        {
            throw new NotFoundException();
        }

        if (string.IsNullOrEmpty(result.ParsedSeriesName))
        {
            throw new BadRequestException("Parsed series name is required");
        }

        if (result is { ParsedHardcoverId: <= 0, ParsedMangaBakaId: <= 0 })
        {
            throw new BadRequestException("Parsed Hardcover or MangaBaka id is required");
        }

        var monitoredSeriesId = await monitoredSeriesService.CreateMonitoredSeries(new CreateOrUpdateMonitoredSeriesDto
        {
            Title = result.ParsedSeriesName,
            ValidTitles = [result.ParsedSeriesName],
            Provider = Provider.Nyaa,
            BaseDir = scan.RootDir,
            ContentFormat = ContentFormat.Manga,
            Format = Format.Archive,
            HardcoverId = result.ParsedHardcoverId > 0 ? result.ParsedHardcoverId.ToString() : string.Empty,
            MangaBakaId = result.ParsedMangaBakaId > 0 ? result.ParsedMangaBakaId.ToString() : string.Empty,
            ExternalId = string.Empty,
            TitleOverride = result.ParsedSeriesName,
            Metadata = new MetadataBag(),
        }, cancellationToken);

        var max = await unitOfWork.ImportScanRepository
            .GetMaxQueuePosition(result.ImportScanId, DirectoryImportStatus.Imported, cancellationToken);

        result.QueuePosition = max + 1;
        result.Status = DirectoryImportStatus.Imported;
        result.MonitoredSeriesId = monitoredSeriesId;
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
