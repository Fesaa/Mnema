using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.External;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Server.Controllers;

public class MetadataController(IServiceProvider serviceProvider): BaseApiController
{

    [HttpGet("get-series")]
    public async Task<ActionResult<Series>> GetSeriesMetadataById([FromQuery] MetadataProvider provider,
        [FromQuery] string externalId)
    {
        var metadataService = serviceProvider.GetKeyedService<IMetadataProviderService>(provider);
        if (metadataService == null)
            return NotFound();

        return Ok(await metadataService.GetSeries(externalId, HttpContext.RequestAborted));
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<Series>>> SearchSeries([FromQuery] MetadataProvider provider,
        [FromQuery] string query, [FromQuery] PaginationParams pagingParams)
    {
        var metadataService = serviceProvider.GetKeyedService<IMetadataProviderService>(provider);
        if (metadataService == null)
            return NotFound();

        return Ok(await metadataService.Search(new MetadataSearchDto(query), pagingParams, HttpContext.RequestAborted));
    }



}
