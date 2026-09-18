using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using J2N.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Providers.Cleanup;
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
    public async Task<IActionResult> Upload(Guid id, CancellationToken ct)
    {
        if (!MediaTypeHeaderValue.TryParse(Request.ContentType, out var mediaTypeHeader) ||
            !mediaTypeHeader.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Expected a multipart request.");
        }

        var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;
        if (string.IsNullOrEmpty(boundary))
        {
            return BadRequest("Missing multipart boundary.");
        }

        var monitoredSeries = await unitOfWork.MonitoredSeriesRepository.GetById(id, ct: ct);
        if (monitoredSeries is null) return NotFound();

        var downloadDirectory = fileSystem.Path.Join(configuration.DownloadDir, id.ToString());

        var filePaths = await CopyToDisk(boundary, downloadDirectory, ct);

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
                    FullPath = path.RemovePrefix(configuration.DownloadDir),
                    FileSize = fileSystem.FileInfo.New(path).Length,
                    VolumeMarker = parseResult.VolumeMarker,
                    ChapterMarker = parseResult.ChapterMarker,
                    Selected = true,
                };
            }).ToList(),
        };

        unitOfWork.DroppedContentRepository.Add(droppedContent);
        await unitOfWork.CommitAsync(ct);

        BackgroundJob.Enqueue(() =>
            cleanupService.CleanupAsync(new DroppedContentAdaptor(droppedContent), CancellationToken.None));

        return Ok();
    }

    private async Task<List<string>> CopyToDisk(string boundary,string downloadDirectory, CancellationToken ct)
    {
        List<string> files = [];

        var reader = new MultipartReader(boundary, Request.Body);

        while (await reader.ReadNextSectionAsync(ct) is { } section)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
            {
                continue;
            }

            if (!contentDisposition.DispositionType.Equals("form-data") ||
                string.IsNullOrEmpty(contentDisposition.FileName.Value)) continue;

            // Prevent directory traversal attacks by taking only the filename
            var safeFileName = fileSystem.Path.GetFileName(contentDisposition.FileName.Value);
            var destinationPath = fileSystem.Path.Combine(downloadDirectory, safeFileName);

            await using var targetStream = fileSystem.File.Create(destinationPath);
            await section.Body.CopyToAsync(targetStream, ct);

            files.Add(destinationPath);
        }

        return files;
    }

}
