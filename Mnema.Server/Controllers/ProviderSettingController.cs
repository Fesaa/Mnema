using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

[Authorize(Roles.ManageSettings)]
public class ProviderSettingsController(IUnitOfWork unitOfWork, IProviderSettingsService providerSettingsService): BaseApiController
{

    [HttpGet]
    public async Task<ActionResult<MetadataBag>> GetProviderSettings([FromQuery] Provider provider)
    {
        var providerSettings = await unitOfWork.ProviderSettingsRepository.GetSettingsForProvider(provider, HttpContext.RequestAborted);
        return Ok(providerSettings.Settings);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProviderSettings([FromQuery] Provider provider,
        [FromBody] MetadataBag settings)
    {
        await providerSettingsService.UpdateSettings(provider, settings, HttpContext.RequestAborted);

        return Ok();
    }


}
