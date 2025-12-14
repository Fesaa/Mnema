using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Providers;

namespace Mnema.Server.Controllers;

public class ContentController(ISearchService searchService, IDownloadService downloadService): BaseApiController
{

    [HttpPost]
    [AllowAnonymous]
    public Task<PagedList<SearchResult>> Search(SearchRequest searchRequest, [FromQuery] PaginationParams? pagination)
    {
        pagination ??= PaginationParams.Default;

        return searchService.Search(searchRequest, pagination, HttpContext.RequestAborted);
    }

    [AllowAnonymous]
    [HttpPost("download")]
    public async Task<IActionResult> Download(DownloadRequestDto request)
    {
        request.UserId = Guid.Parse("2f461b21-85f0-4e1e-b64b-fb48c774cdb6");
        
        await downloadService.StartDownload(request);

        return Ok();
    }
    
    [AllowAnonymous]
    [HttpGet("stats")]
    public async Task<ActionResult<IEnumerable<DownloadInfo>>> Stats()
    {
        return Ok(await downloadService.GetCurrentContent());
    }
    
    [AllowAnonymous]
    [HttpPost("stop")]
    public async Task<IActionResult> Stop(StopRequestDto request)
    {
        request.UserId = Guid.Parse("2f461b21-85f0-4e1e-b64b-fb48c774cdb6");

        await downloadService.CancelDownload(request);
        
        return Ok();
    }
    
}