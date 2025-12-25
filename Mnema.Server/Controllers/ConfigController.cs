using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Models.DTOs;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

public class ConfigController(ILogger<ConfigController> logger, ISettingsService settingsService): BaseApiController
{

    [HttpGet]
    public async Task<ActionResult<ServerSettingsDto>> GetSettings()
    {
        var dto = await settingsService.GetSettingsAsync();

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles.ManageSettings)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateServerSettingsDto dto)
    {
        await settingsService.SaveSettingsAsync(dto);

        return Ok();
    }

}