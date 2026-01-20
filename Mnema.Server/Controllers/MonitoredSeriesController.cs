using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Models.Publication;

namespace Mnema.Server.Controllers;

[Authorize(Roles.Subscriptions)]
public class MonitoredSeriesController(
    IUnitOfWork unitOfWork,
    IMonitoredSeriesService monitoredSeriesService,
    IMetadataResolver metadataResolver
) : BaseApiController
{
    [HttpGet("all")]
    public async Task<ActionResult<PagedList<MonitoredSeriesDto>>> GetAll([FromQuery] string query = "",
        [FromQuery] PaginationParams? paginationParams = null)
    {
        paginationParams ??= PaginationParams.Default;

        return Ok(await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeriesDtosForUser(UserId, query, paginationParams, HttpContext.RequestAborted));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MonitoredSeriesDto>> Get(Guid id)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeriesDto(id, HttpContext.RequestAborted);
        if (series == null) return NotFound();

        if (series.UserId != UserId) return Forbid();

        return Ok(series);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] CreateOrUpdateMonitoredSeriesDto updateDto)
    {
        await monitoredSeriesService.UpdateMonitoredSeries(UserId, updateDto, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpPost("new")]
    public async Task<IActionResult> Create([FromBody] CreateOrUpdateMonitoredSeriesDto createDto)
    {
        await monitoredSeriesService.CreateMonitoredSeries(UserId, createDto, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpGet("{id:guid}/resolved-series")]
    public async Task<ActionResult<Series>> GetResolvedSeries(Guid id)
    {
        var monitoredSeries = await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeries(id, HttpContext.RequestAborted);
        if (monitoredSeries == null) return NotFound();

        if (monitoredSeries.UserId != UserId) return Forbid();

        var series = await metadataResolver.ResolveSeriesAsync(monitoredSeries.MetadataForDownloadRequest(), HttpContext.RequestAborted);

        return Ok(series);
    }

    [HttpPost("{id:guid}/refresh-metadata")]
    public async Task<ActionResult<MonitoredSeriesDto>> RefreshMetadata(Guid id)
    {
        var monitoredSeries = await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeries(id, HttpContext.RequestAborted);
        if (monitoredSeries == null) return NotFound();

        if (monitoredSeries.UserId != UserId) return Forbid();

        await monitoredSeriesService.EnrichWithMetadata(monitoredSeries, HttpContext.RequestAborted);

        await unitOfWork.CommitAsync();

        return Ok(await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeriesDto(id, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeries(id, HttpContext.RequestAborted);
        if (series == null) return NotFound();

        if (series.UserId != UserId) return Forbid();

        unitOfWork.MonitoredSeriesRepository.Remove(series);

        await unitOfWork.CommitAsync();

        return Ok();
    }

    [HttpPost("{id:guid}/{chapterId:guid}/set-status")]
    public async Task<IActionResult> SetChapterStatus(Guid id, Guid chapterId, [FromQuery] MonitoredChapterStatus status)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeries(id, HttpContext.RequestAborted);
        if (series == null) return NotFound();

        if (series.UserId != UserId) return Forbid();

        var chapter = series.Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter == null) return NotFound();

        chapter.Status = status;

        await unitOfWork.CommitAsync();

        return Ok();
    }

    [HttpGet("form")]
    public ActionResult<FormDefinition> GetForm()
    {
        return Ok(monitoredSeriesService.GetForm());
    }

    [HttpGet("{id:guid}/metadata-form")]
    public async Task<ActionResult<FormDefinition>> GetMetadataForm(Guid id)
    {
        return Ok(await monitoredSeriesService.GetMetadataForm(UserId, id, HttpContext.RequestAborted));
    }
}
