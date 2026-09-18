using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using J2N.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Providers.Managers.Dropped;

namespace Mnema.Server.Controllers;

public class DroppedContentController(
    IUnitOfWork unitOfWork,
    IFileSystem fileSystem,
    ApplicationConfiguration configuration,
    IParserService parserService,
    [FromKeyedServices(ICleanupService.RawFileCleanupServiceKey)] ICleanupService cleanupService
    ): BaseApiController
{

    private const long MaxFileSizeInBytes = 512 * 1024 * 1024; // 512 MB

    [HttpPost("{id:guid}/upload")]
    [RequestSizeLimit(MaxFileSizeInBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeInBytes)]
    public async Task<IActionResult> Upload(Guid id, List<IFormFile> files, CancellationToken ct)
    {
        if (files.Count <= 0) return BadRequest();

        if (await unitOfWork.DroppedContentRepository.ExistsByMonitoredSeriesIdAsync(id, ct))
        {
            return BadRequest("A dropped content import for this monitored series is already running. Please wait");
        }

        var monitoredSeries = await unitOfWork.MonitoredSeriesRepository.GetById(id, ct: ct);
        if (monitoredSeries is null) return NotFound();

        var downloadDirectory = fileSystem.Path.Join(configuration.DownloadDir, id.ToString());
        if (!fileSystem.Directory.Exists(downloadDirectory))
        {
            fileSystem.Directory.CreateDirectory(downloadDirectory);
        }

        List<string> filePaths = [];
        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            var safeFileName = fileSystem.Path.GetFileName(file.FileName);
            var destinationPath = fileSystem.Path.Combine(downloadDirectory, safeFileName);

            await using var targetStream = fileSystem.File.Create(destinationPath);
            await file.CopyToAsync(targetStream, ct);

            filePaths.Add(destinationPath);
        }

        var droppedContent = new DroppedContent
        {
            MonitoredSeriesId = id,
            MonitoredSeries = monitoredSeries,
            Files = filePaths.Select(path =>
            {
                var fileName = fileSystem.Path.GetFileName(path);
                var parseResult = parserService.FullParse(fileName, monitoredSeries.ContentFormat);

                return new DownloadFile
                {
                    FileName = fileName,
                    FullPath = fileName,
                    FileSize = fileSystem.FileInfo.New(path).Length,
                    VolumeMarker = parseResult.VolumeMarker,
                    ChapterMarker = parseResult.ChapterMarker,
                    Selected = true,
                };
            }).ToList(),
        };

        unitOfWork.DroppedContentRepository.Add(droppedContent);
        await unitOfWork.CommitAsync(ct);

        BackgroundJob.Enqueue(() => Cleanup(droppedContent.Id, CancellationToken.None));

        return Ok();
    }

    public async Task Cleanup(Guid id, CancellationToken ct)
    {
        var droppedContent = await unitOfWork.DroppedContentRepository.GetById(id, ct);
        if (droppedContent is null) return;

        await cleanupService.CleanupAsync(new DroppedContentAdaptor(droppedContent), ct);

        var downloadDirectory = fileSystem.Path.Join(configuration.DownloadDir, droppedContent.MonitoredSeriesId.ToString());
        if (fileSystem.Directory.Exists(downloadDirectory))
        {
            fileSystem.Directory.Delete(downloadDirectory, true);
        }

        await unitOfWork.DroppedContentRepository.DeleteById(id, ct);
    }

}
