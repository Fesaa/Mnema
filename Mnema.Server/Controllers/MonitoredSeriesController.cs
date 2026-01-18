using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

[Authorize(Roles.Subscriptions)]
public class MonitoredSeriesController(
    IUnitOfWork unitOfWork,
    IMonitoredSeriesService monitoredSeriesService
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

    [HttpGet("form")]
    public ActionResult<FormDefinition> GetForm()
    {
        return Ok(monitoredSeriesService.GetForm());
    }
}
