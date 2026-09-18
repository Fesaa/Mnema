using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.API.External;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.Internal;
using Mnema.Models.Publication;
using Mnema.Providers.Managers.Dropped;
using Mnema.Server.Configuration;

namespace Mnema.Server.Controllers;

[Authorize(Roles.Subscriptions)]
public class MonitoredSeriesController(
    IUnitOfWork unitOfWork,
    IMonitoredSeriesService monitoredSeriesService,
    IMetadataResolver metadataResolver,
    IMessageService messageService,
    ISearchService searchService,
    IDownloadService downloadService,
    IConnectionService connectionService,
    IParserService parserService,
    IMetadataService metadataService,
    IFileSystem fileSystem,
    [FromKeyedServices(ICleanupService.RawFileCleanupServiceKey)] ICleanupService cleanupService,
    ApplicationConfiguration configuration
) : BaseApiController
{

    private const long MaxFileSizeInBytes = 512 * 1024 * 1024; // 512 MB

    [HttpGet("all")]
    public async Task<ActionResult<PagedList<MonitoredSeriesDto>>> GetAll([FromQuery] string query = "",
        [FromQuery] Provider? provider = null,
        [FromQuery] PaginationParams? paginationParams = null)
    {
        paginationParams ??= PaginationParams.Default;

        return Ok(await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeriesDtosForUser(query, provider, paginationParams, HttpContext.RequestAborted));
    }

    [HttpGet("providers")]
    public async Task<ActionResult<List<Provider>>> InUseProviders()
    {
        return Ok(await unitOfWork.MonitoredSeriesRepository.GetProviders(HttpContext.RequestAborted));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MonitoredSeriesDto>> Get(Guid id)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetDtoById(id, MonitoredSeriesIncludes.Chapters, HttpContext.RequestAborted);
        if (series == null) return NotFound();

        return Ok(series);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] CreateOrUpdateMonitoredSeriesDto updateDto)
    {
        await monitoredSeriesService.UpdateMonitoredSeries(updateDto, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpPost("new")]
    public async Task<IActionResult> Create([FromBody] CreateOrUpdateMonitoredSeriesDto createDto)
    {
        await monitoredSeriesService.CreateMonitoredSeries(createDto, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpGet("{id:guid}/resolved-series")]
    public async Task<ActionResult<Series>> GetResolvedSeries(Guid id)
    {
        var monitoredSeries = await unitOfWork.MonitoredSeriesRepository.GetById(id, MonitoredSeriesIncludes.Chapters, HttpContext.RequestAborted);
        if (monitoredSeries == null) return NotFound();

        var series = await metadataResolver.ResolveSeriesAsync(monitoredSeries.Provider, monitoredSeries.MetadataForDownloadRequest(), HttpContext.RequestAborted);

        return Ok(series);
    }

    [HttpPost("{id:guid}/refresh-metadata")]
    public async Task<IActionResult> RefreshMetadata(Guid id)
    {
        var monitoredSeries = await unitOfWork.MonitoredSeriesRepository.GetById(id, MonitoredSeriesIncludes.Chapters, HttpContext.RequestAborted);
        if (monitoredSeries == null) return NotFound();

        BackgroundJob.Enqueue(() => monitoredSeriesService.EnrichWithMetadata(id, cancellationToken: CancellationToken.None));

        return Ok();
    }

    [HttpGet("{id:guid}/search")]
    public async Task<ActionResult<PagedList<SearchResult>>> Search(Guid id, [FromQuery] PaginationParams paginationParams)
    {
        var mSeries = await unitOfWork.MonitoredSeriesRepository.GetById(id, MonitoredSeriesIncludes.Chapters, HttpContext.RequestAborted);
        if (mSeries == null) return NotFound();

        var req = new SearchRequest
        {
            Provider = mSeries.Provider,
            Query = mSeries.Metadata.GetKey(RequestConstants.TitleOverride) ?? mSeries.Title,
            Modifiers = mSeries.MetadataForDownloadRequest()
        };

        return Ok(await searchService.Search(req, paginationParams, HttpContext.RequestAborted));
    }

    [HttpPost("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, SearchResult result)
    {
        var mSeries = await unitOfWork.MonitoredSeriesRepository.GetById(id, MonitoredSeriesIncludes.Chapters, HttpContext.RequestAborted);
        if (mSeries == null) return NotFound();

        if (mSeries.Provider != result.Provider) return BadRequest();

        var req = new DownloadRequestDto
        {
            Provider = result.Provider,
            Id = result.Id,
            BaseDir = mSeries.BaseDir,
            TempTitle = mSeries.Title,
            Metadata = mSeries.MetadataForDownloadRequest(),
            DownloadUrl = result.DownloadUrl,
            StartImmediately = true
        };

        await downloadService.StartDownload(req);

        return Ok();
    }

    [HttpPost("{id:guid}/download-external-id")]
    public async Task<IActionResult> DownloadExternalId(Guid id)
    {
        var mSeries = await unitOfWork.MonitoredSeriesRepository.GetById(id, MonitoredSeriesIncludes.Chapters, HttpContext.RequestAborted);
        if (mSeries == null) return NotFound();

        if (string.IsNullOrWhiteSpace(mSeries.ExternalId)) return BadRequest();

        await monitoredSeriesService.StartDownload(id, false, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetById(id, MonitoredSeriesIncludes.Chapters, HttpContext.RequestAborted);
        if (series == null) return NotFound();

        unitOfWork.MonitoredSeriesRepository.Remove(series);

        await unitOfWork.CommitAsync();

        await connectionService.CommunicateSeriesUnmonitored(series.Id);

        return Ok();
    }

    [HttpPost("{id:guid}/{chapterId:guid}/set-status")]
    public async Task<IActionResult> SetChapterStatus(Guid id, Guid chapterId, [FromQuery] MonitoredChapterStatus status)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetById(id, MonitoredSeriesIncludes.Chapters, HttpContext.RequestAborted);
        if (series == null) return NotFound();

        var chapter = series.Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter == null) return NotFound();

        chapter.Status = status;

        await unitOfWork.CommitAsync();

        return Ok();
    }

    [HttpGet("missing-chapters")]
    public async Task<ActionResult<PagedList<MonitoredChapterDto>>> GetMissingChapters(
        [FromQuery] PaginationParams pagination)
    {
        return Ok(await unitOfWork.MonitoredSeriesRepository.GetMissingChapters(pagination, HttpContext.RequestAborted));
    }

    [HttpGet("{id:guid}/file-info")]
    public async Task<ActionResult<FileInfoDto?>> GetFileInfo(Guid id, [FromQuery] string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return BadRequest();

        var series = await unitOfWork.MonitoredSeriesRepository.GetById(id, MonitoredSeriesIncludes.Chapters, HttpContext.RequestAborted);
        if (series == null) return NotFound();

        var chapter = series.Chapters.FirstOrDefault(c => c.FilePath == filePath);
        if (chapter != null)
        {
            return Ok(new FileInfoDto
            {
                Path = chapter.FilePath!,
                Volume = chapter.Volume,
                Chapter = chapter.Chapter,
                Metadata = FileMetadataDto.FromComicInfo(chapter.ComicInfo)
            });
        }

        var unMatchedChapter = series.UnMatchedChapters.FirstOrDefault(c => c.Path == filePath);
        if (unMatchedChapter != null)
        {
            return Ok(new FileInfoDto
            {
                Path = unMatchedChapter.Path,
                Volume = unMatchedChapter.VolumeMarker,
                Chapter = unMatchedChapter.ChapterMarker,
                Metadata = FileMetadataDto.FromComicInfo(unMatchedChapter.ComicInfo)
            });
        }

        return NotFound();
    }

    [HttpGet("{id:guid}/{chapterId:guid}/metadata")]
    [OutputCache(PolicyName = CacheProfiles.FiveMinutes)]
    public async Task<ActionResult<FileMetadataDto>> GetFileMetadata(Guid id, Guid chapterId)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetById(id, MonitoredSeriesIncludes.Chapters, HttpContext.RequestAborted);
        if (series == null) return NotFound();

        if (string.IsNullOrEmpty(series.TitleOverride))
            return BadRequest("Monitored series requires a title override to support metadata changes");

        MonitoredChapter? chapter = null;
        if (chapterId != Guid.Empty)
        {
            chapter = series.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (chapter == null) return BadRequest("Chapter not found");
        }

        var metadata = series.MetadataForDownloadRequest();
        var resolvedSeries = await metadataResolver.ResolveSeriesAsync(series.Provider, metadata, HttpContext.RequestAborted);
        if (resolvedSeries == null) return NotFound();

        Chapter? resolvedChapter = null;
        if (chapter is not null)
        {
            resolvedChapter = parserService.FindMatch(resolvedSeries.Chapters, chapter);
            if (resolvedChapter == null) return NotFound();
        }

        var preferences = await unitOfWork.SettingsRepository.GetPreferencesAsync(HttpContext.RequestAborted);

        var ci = metadataService.CreateComicInfo(preferences, new DownloadRequestDto
        {
            Provider = series.Provider,
            Id = series.ExternalId,
            BaseDir = series.BaseDir,
            TempTitle = series.TitleOverride,
            Metadata = metadata
        }, series.TitleOverride, resolvedSeries, resolvedChapter);

        return Ok(FileMetadataDto.FromComicInfo(ci));
    }

    [HttpPost("{id:guid}/file-metadata")]
    public async Task<IActionResult> WriteFileMetadata(Guid id, [FromQuery] string filePath, [FromBody] FileMetadataDto metadata)
    {
        if (string.IsNullOrEmpty(filePath)) return BadRequest();

        await monitoredSeriesService.UpdateFileMetadata(id, filePath, metadata, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpGet("form")]
    public ActionResult<FormDefinition> GetForm()
    {
        return Ok(monitoredSeriesService.GetForm());
    }

    [HttpGet("metadata-form")]
    public async Task<ActionResult<FormDefinition>> GetMetadataForm([FromQuery] Provider provider)
    {
        return Ok(await monitoredSeriesService.GetMetadataForm(provider, HttpContext.RequestAborted));
    }

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

    [AutomaticRetry(Attempts = 0)]
    [Queue(HangfireQueue.TorrentCleanup)]
    [DisableConcurrentExecution(timeoutInSeconds: 86400 * 2)]
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
