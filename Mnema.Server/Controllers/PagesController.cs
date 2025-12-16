using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Server.Configuration;

namespace Mnema.Server.Controllers;

public class PagesController(ILogger<PagesController> logger, IUnitOfWork unitOfWork, IPagesService pagesService): BaseApiController
{

    /// <summary>
    /// Returns the pages the currently active user has access to
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IList<PageDto>>> GetPages()
    {
        var pages = await unitOfWork.PagesRepository.GetPageDtosForUser(UserId);
        
        return Ok(pages);
    }

    [HttpGet("download-metadata")]
    [ResponseCache(CacheProfileName = CacheProfiles.OneHour, VaryByHeader = "providers")]
    public async Task<IActionResult> DownloadMetadata([FromQuery] Provider providers)
    {
        return Ok();
    }

    [HttpPost("new")]
    [HttpPost("update")]
    [Authorize(Roles.ManagePages)]
    public async Task<IActionResult> UpdatePages([FromBody] PageDto dto)
    {
        await pagesService.UpdatePage(dto);

        return Ok();
    }

    [Authorize(Roles.ManagePages)]
    [HttpDelete("{pageId:guid}")]
    public async Task<IActionResult> DeletePage(Guid pageId)
    {
        await unitOfWork.PagesRepository.DeletePage(pageId);

        return Ok();
    }

    [HttpPost("order")]
    [Authorize(Roles.ManagePages)]
    public async Task<IActionResult> OrderPages([FromBody] Guid[] ids)
    {
        await pagesService.OrderPages(ids);

        return Ok();
    }

    [HttpPost("load-default")]
    [Authorize(Roles.ManagePages)]
    public async Task<IActionResult> LoadDefaults()
    {
        throw new NotImplementedException();
    }
    
}