using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.API.Repositories;
using Mnema.Common;
using Mnema.Models.DTOs.Scanner;

namespace Mnema.Server.Controllers;

public class ImportScanController(ILogger<ImportScanController> logger, IUnitOfWork unitOfWork): BaseApiController
{

    [HttpPost("start-import-scan")]
    public IActionResult StartImportScan([FromBody] StartScanDto dto)
    {
        if (dto.RootDir.Contains(".."))
        {
            return BadRequest();
        }

        BackgroundJob.Enqueue<IScannerService>(s => s.ScanRoot(dto.RootDir, CancellationToken.None));

        return Ok();
    }

    [HttpGet("shallow-paged")]
    public async Task<PagedList<ImportScanShallowDto>> GetShallowPaged([FromQuery] PaginationParams paginationParams)
    {
        return await unitOfWork.ImportScanRepository.GetShallowScansPaged(paginationParams, HttpContext.RequestAborted);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ImportScanDto?>> GetById(Guid id)
    {
        var scan = await unitOfWork.ImportScanRepository.GetDtoById(id,
            ImportScanIncludes.DirectoryImports | ImportScanIncludes.ImportErrors, HttpContext.RequestAborted);
        if (scan == null) return NotFound();

        return Ok(scan);
    }

}
