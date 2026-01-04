using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.API.External;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.External;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

[Authorize(Roles.ManageExternalConnections)]
public class ExternalConnectionController(IUnitOfWork unitOfWork, IExternalConnectionService externalConnectionService)
    : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedList<ExternalConnectionDto>>> GetExternalConnections(
        [FromQuery] PaginationParams paginationParams)
    {
        return Ok(await unitOfWork.ExternalConnectionRepository.GetAllConnectionDtos(paginationParams,
            HttpContext.RequestAborted));
    }

    [HttpGet("form")]
    public async Task<ActionResult<FormDefinition>> GetFormDefinition([FromQuery] ExternalConnectionType type)
    {
        var form = await externalConnectionService.GetForm(type, HttpContext.RequestAborted);

        return Ok(form);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateConnection(ExternalConnectionDto dto)
    {
        await externalConnectionService.UpdateConnection(dto, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteConnection(Guid id, CancellationToken cancellationToken)
    {
        await unitOfWork.ExternalConnectionRepository.DeleteConnectionById(id, cancellationToken);

        return Ok();
    }
}