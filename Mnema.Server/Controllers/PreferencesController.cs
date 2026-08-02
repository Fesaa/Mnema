using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Models.DTOs;

namespace Mnema.Server.Controllers;

public class PreferencesController(
    ILogger<PreferencesController> logger,
    IUnitOfWork unitOfWork,
    ISettingsService settingsService,
    IMapper mapper) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<PreferencesDto>> GetPreferences()
    {
        var pref = await unitOfWork.SettingsRepository.GetPreferencesAsync(HttpContext.RequestAborted);
        return Ok(mapper.Map<PreferencesDto>(pref));
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePreferences([FromBody] PreferencesDto dto)
    {
        await settingsService.UpdatePreferences(dto, HttpContext.RequestAborted);

        return Ok();
    }
}
