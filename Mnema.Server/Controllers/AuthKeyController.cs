using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs.UI;
using Mnema.Models.DTOs.User;

namespace Mnema.Server.Controllers;

public class AuthKeyController(IAuthKeyService authKeyService, IUnitOfWork unitOfWork): BaseApiController
{

    [HttpGet]
    public async Task<ActionResult<PagedList<AuthKeyDto>>> GetAuthKeysByUser([FromQuery] PaginationParams paginationParams)
    {
        return Ok(await unitOfWork.AuthKeyRepository.GetAuthKeysByUser(UserId, paginationParams, HttpContext.RequestAborted));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var authKey = await unitOfWork.AuthKeyRepository.GetById(id, HttpContext.RequestAborted);
        if (authKey == null) return NotFound();

        if (authKey.UserId != UserId) return Forbid();

        unitOfWork.AuthKeyRepository.Remove(authKey);
        await unitOfWork.CommitAsync();

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AuthKeyDto dto)
    {
        await authKeyService.CreateAuthKey(UserId, dto, User, HttpContext.RequestAborted);

        return Ok();
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AuthKeyDto dto)
    {
        await authKeyService.UpdateAuthKey(UserId, dto, User, HttpContext.RequestAborted);

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
