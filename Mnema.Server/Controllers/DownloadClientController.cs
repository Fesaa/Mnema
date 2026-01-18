using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;

namespace Mnema.Server.Controllers;

public class DownloadClientController(IUnitOfWork unitOfWork, IDownloadClientService downloadClientService): BaseApiController
{

    [HttpGet]
    public async Task<ActionResult<PagedList<DownloadClientDto>>> GetAll(PaginationParams paginationParams)
    {
        var clients =
            await unitOfWork.DownloadClientRepository.GetAllDownloadClientsAsync(paginationParams,
                HttpContext.RequestAborted);

        return Ok(clients);
    }

    [HttpGet("available-types")]
    public async Task<ActionResult<List<DownloadClientType>>> GetFreeTypes()
    {
        var inUse = (await unitOfWork.DownloadClientRepository
            .GetInUseTypesAsync(HttpContext.RequestAborted))
            .ToHashSet();

        var free = Enum.GetValues<DownloadClientType>()
            .Where(t => !inUse.Contains(t))
            .ToList();

        return Ok(free);
    }

    [HttpDelete("{id:guid}/failed-lock")]
    public async Task<IActionResult> ReleaseFailedLock(Guid id)
    {
        await downloadClientService.ReleaseFailedLock(id, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrCreat(DownloadClientDto downloadClientDto)
    {
        await downloadClientService.UpdateDownloadClientAsync(downloadClientDto, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpGet("form")]
    public async Task<ActionResult<FormDefinition>> GetForm([FromQuery] DownloadClientType type)
    {
        var form = await downloadClientService.GetFormDefinitionForType(type, HttpContext.RequestAborted);

        return Ok(form);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDownloadClient(Guid id)
    {
        await unitOfWork.DownloadClientRepository.DeleteByIdAsync(id, HttpContext.RequestAborted);

        return Ok();
    }


}
