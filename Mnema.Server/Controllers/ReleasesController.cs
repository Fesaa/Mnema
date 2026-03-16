using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

[Authorize(Roles.ManageSettings)]
public class ReleasesController(IUnitOfWork unitOfWork): BaseApiController
{

    [HttpGet("releases")]
    public async Task<ActionResult<PagedList<ContentReleaseDto>>> GetReleases([FromQuery] PaginationParams paginationParams, [FromQuery] string? query = null)
    {
        return Ok(await unitOfWork.ContentReleaseRepository.GetReleases(query, paginationParams, HttpContext.RequestAborted));
    }

    [HttpGet("imported")]
    public async Task<ActionResult<PagedList<ContentReleaseDto>>> GetImported([FromQuery] PaginationParams paginationParams, [FromQuery] string? query = null)
    {
        return Ok(await unitOfWork.ImportedReleaseRepository.GetReleases(query, paginationParams, HttpContext.RequestAborted));
    }

    [HttpDelete("{uuid:guid}")]
    public async Task<IActionResult> Delete(Guid uuid)
    {
        await unitOfWork.ContentReleaseRepository.Delete(uuid, HttpContext.RequestAborted);

        return Ok();
    }


}
