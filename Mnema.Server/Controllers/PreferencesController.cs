using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.UI;

namespace Mnema.Server.Controllers;

public class PreferencesController(
    ILogger<PreferencesController> logger,
    IUnitOfWork unitOfWork,
    ISettingsService settingsService,
    IMapper mapper,
    INamingService namingService
    ) : BaseApiController
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

    [HttpPost("valid-chapter-format")]
    public ActionResult IsValidChapterFormat([FromBody] FormFieldValidationRequestDto<string> validationRequest)
    {
        var errors = namingService.ChapterFormatter.Validate(validationRequest.FormValue);
        if (errors.Count == 0) return Ok(null);

        return Ok(new
        {
            invalidFormat = errors
        });
    }

    [HttpPost("valid-one-shot-format")]
    public ActionResult IsValidOneShotFormat([FromBody] FormFieldValidationRequestDto<string> validationRequest)
    {
        var errors = namingService.OneShotFormatter.Validate(validationRequest.FormValue);
        if (errors.Count == 0) return Ok(null);

        return Ok(new
        {
            invalidFormat = errors
        });
    }
}
