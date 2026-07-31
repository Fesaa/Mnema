using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Services;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

public class ConfigController(ILogger<ConfigController> logger, ISettingsService settingsService, IUnitOfWork unitOfWork, IPasswordService passwordService) : BaseApiController
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

    [AllowAnonymous]
    [HttpPost("set-password")]
    public async Task<ActionResult> SetPassword()
    {
        var form = await HttpContext.Request.ReadFormAsync();
        if (!form.TryGetValue("password", out var passwordValues))
        {
            return BadRequest();
        }

        var password = passwordValues.FirstOrDefault();
        if (string.IsNullOrEmpty(password)) return BadRequest();

        var passwordSetting = await unitOfWork.SettingsRepository.GetSettingsAsync(ServerSettingKey.Password);
        if (!string.IsNullOrEmpty(passwordSetting.Value)) return BadRequest();

        passwordSetting.Value = passwordService.HashPassword(password);
        unitOfWork.SettingsRepository.Update(passwordSetting);
        await unitOfWork.CommitAsync();

        return Redirect("/");
    }
}
