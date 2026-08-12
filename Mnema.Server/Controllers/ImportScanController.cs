using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.API.Repositories;
using Mnema.Common;
using Mnema.Models.DTOs.Scanner;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

[Authorize(Roles.ImportScans)]
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

    [HttpGet("paged")]
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

    [HttpGet("{id:guid}/directories")]
    public async Task<PagedList<DirectoryImportResultDto>> GetDirectoryImports(Guid id,
        [FromQuery] PaginationParams paginationParams)
    {
        return await unitOfWork.ImportScanRepository.GetDirectoryImportsPaged(id, paginationParams, HttpContext.RequestAborted);
    }

    [HttpGet("{id:guid}/errors")]
    public async Task<PagedList<ImportErrorDto>> GetErrors(Guid id,
        [FromQuery] PaginationParams paginationParams)
    {
        return await unitOfWork.ImportScanRepository.GetImportErrorsPaged(id, paginationParams, HttpContext.RequestAborted);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await unitOfWork.ImportScanRepository.DeleteById(id);

        return Ok();
    }

}
