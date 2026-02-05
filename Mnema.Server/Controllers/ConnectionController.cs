using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

[Authorize(Roles.ManageExternalConnections)]
public class ConnectionController(IUnitOfWork unitOfWork, IConnectionService connectionService)
    : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PagedList<ConnectionDto>>> GetExternalConnections(
        [FromQuery] PaginationParams paginationParams)
    {
        return Ok(await unitOfWork.ConnectionRepository.GetAllDtosPaged(paginationParams,
            HttpContext.RequestAborted));
    }

    [HttpGet("form")]
    public async Task<ActionResult<FormDefinition>> GetFormDefinition([FromQuery] ConnectionType type)
    {
        var form = await connectionService.GetForm(type, HttpContext.RequestAborted);

        return Ok(form);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateConnection(ConnectionDto dto)
    {
        await connectionService.UpdateConnection(dto, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteConnection(Guid id, CancellationToken cancellationToken)
    {
        await unitOfWork.ConnectionRepository.DeleteById(id, cancellationToken);

        return Ok();
    }
}
