using Microsoft.AspNetCore.Mvc;
using Mnema.API.Database;
using Mnema.Models.DTOs.UI;

namespace Mnema.Server.Controllers;

public class PagesController(ILogger<PagesController> logger, IUnitOfWork unitOfWork): BaseApiController
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
    
}