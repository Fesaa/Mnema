using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Models.DTOs.User;

namespace Mnema.Server.Controllers;

public class PreferencesController(ILogger<PreferencesController> logger, IUnitOfWork unitOfWork, IUserService userService, IMapper mapper): BaseApiController
{

    [HttpGet]
    public async Task<ActionResult<UserPreferencesDto>> GetPreferences()
    {
        var pref = await unitOfWork.UserRepository.GetPreferences(UserId);
        if (pref == null) return NotFound();
        
        return Ok(mapper.Map<UserPreferencesDto>(pref));
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferencesDto dto)
    {
        await userService.UpdatePreferences(UserId, dto);

        return Ok();
    }
    
}