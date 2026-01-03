using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.IO;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

// TODO: Scope I/O access to user permissions
//  I.e. can only list & create in directories with their base in a page they have access to
public class IoController(
    ILogger<IoController> logger,
    ApplicationConfiguration applicationConfiguration,
    IFileSystem fileSystem
    ): BaseApiController
{

    [HttpPost("ls")]
    public ActionResult<List<ListDirEntryDto>> ListDir(ListDirRequestDto request)
    {
        if (request.Directory.Contains(".."))
            return BadRequest();

        var dir = fileSystem.Path.Join(applicationConfiguration.BaseDir, request.Directory);

        var dirEntries = fileSystem.Directory.EnumerateFileSystemEntries(dir)
            .Select(entry =>
            {
                var isDirectory = Directory.Exists(entry);
                if (!(isDirectory || request.ShowFiles))
                {
                    return null;
                }

                return new ListDirEntryDto(fileSystem.Path.GetFileName(entry), isDirectory);
            })
            .WhereNotNull()
            .ToList();

        return Ok(dirEntries);
    }

    [HttpPost("create")]
    [Authorize(Roles.CreateDirectory)]
    public IActionResult CreateDir(CreateDirRequestDto request)
    {
        if (request.BaseDir.Contains("..") || request.NewDir.Contains(".."))
            return BadRequest();

        var baseDir = fileSystem.Path.Join(applicationConfiguration.BaseDir, request.BaseDir);
        if (!fileSystem.Directory.Exists(baseDir))
            return BadRequest("Base directory does not exist");

        var fullDir = fileSystem.Path.Join(baseDir, request.NewDir);

        fileSystem.Directory.CreateDirectory(fullDir);

        return Ok();
    }

}