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

    [HttpPost("download")]
    [AllowAnonymous]
    public async Task<IActionResult> Download(DownloadRequestDto request)
    {
        await downloadService.StartDownload(request);

        return Ok();
    }

    [HttpGet("stats")]
    public IActionResult Stats()
    {
        return Ok();
    }
    
}