using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.UI;

namespace Mnema.Server.Controllers;

public class AuthKeyController(IAuthKeyService authKeyService, IUnitOfWork unitOfWork): BaseApiController
{

    [HttpGet]
    public async Task<ActionResult<PagedList<AuthKeyDto>>> GetAuthKeys([FromQuery] PaginationParams paginationParams)
    {
        return Ok(await unitOfWork.AuthKeyRepository.GetAllDtosPaged(paginationParams, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var authKey = await unitOfWork.AuthKeyRepository.GetById(id, HttpContext.RequestAborted);
        if (authKey == null) return NotFound();

        unitOfWork.AuthKeyRepository.Remove(authKey);
        await unitOfWork.CommitAsync();

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AuthKeyDto dto)
    {
        await authKeyService.CreateAuthKey(dto, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AuthKeyDto dto)
    {
        await authKeyService.UpdateAuthKey(dto, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpGet("form")]
    public ActionResult<FormDefinition> GetForm()
    {
        return Ok(new FormDefinition
        {
            Key = "settings.auth-keys.edit",
            Controls = authKeyService.GetAuthKeyForm(User),
        });
    }

}
