using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.Internal;
using Mnema.Models.Publication;

namespace Mnema.Server.Controllers;

[Authorize(Roles.Subscriptions)]
public class MonitoredSeriesController(
    IUnitOfWork unitOfWork,
    IMonitoredSeriesService monitoredSeriesService,
    IMetadataResolver metadataResolver,
    IMessageService messageService,
    ISearchService searchService,
    IDownloadService downloadService,
    IConnectionService connectionService
) : BaseApiController
{
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
}
