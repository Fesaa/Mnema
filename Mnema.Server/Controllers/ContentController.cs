using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API.Providers;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Providers;

namespace Mnema.Server.Controllers;

public class ContentController([FromKeyedServices(nameof(Provider.Mangadex))] IRepository repository): BaseApiController
{

    [HttpPost]
    [AllowAnonymous]
    public async Task<PagedList<SearchResult>> Search(SearchRequest searchRequest, [FromQuery] PaginationParams? pagination)
    {
        pagination ??= PaginationParams.Default;
        
        return await repository.SearchPublications(searchRequest, pagination, HttpContext.RequestAborted);
    }

    [HttpGet("stats")]
    public IActionResult Stats()
    {
        return Ok();
    }
    
}