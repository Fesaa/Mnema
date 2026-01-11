using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Server.Controllers;

public class MetadataController(IServiceProvider serviceProvider): BaseApiController
{

    [HttpGet("search")]
    public async Task<ActionResult<Series>> GetSeriesMetadataById([FromQuery] MetadataProvider provider,
        [FromQuery] string externalId)
    {
        var metadataService = serviceProvider.GetKeyedService<IMetadataProviderService>(provider);
        if (metadataService == null)
            return NotFound();

        return Ok(await metadataService.GetSeries(externalId, HttpContext.RequestAborted));
    }

}
