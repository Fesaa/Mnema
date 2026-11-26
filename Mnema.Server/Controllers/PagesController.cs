using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Enums;
using Mnema.Models.Internal;
using Mnema.Server.Configuration;

namespace Mnema.Server.Controllers;

public class PagesController(
    ILogger<PagesController> logger,
    IUnitOfWork unitOfWork,
    IPagesService pagesService,
    IServiceProvider serviceProvider) : BaseApiController
{
    /// <summary>
    ///     Returns the pages the currently active user has access to
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IList<PageDto>>> GetPages()
    {
        var pages = await unitOfWork.PagesRepository.GetPageDtosForUser();

        foreach (var page in pages)
        {
            var repository = serviceProvider.GetKeyedService<IContentRepository>(page.Provider);
            if (repository == null)
            {
                logger.LogWarning("Page {Guid} with provider {Provider} could not be enriched", page.Id, page.Provider);
                page.Metadata = [];
                page.Modifiers = [];
                continue;
            }

            page.Metadata = BuildPageFormMetadata(await repository.DownloadMetadata(HttpContext.RequestAborted));
            page.Modifiers = await repository.Modifiers(HttpContext.RequestAborted);
        }

        return Ok(pages);
    }

    [HttpGet("download-metadata")]
    [ResponseCache(CacheProfileName = CacheProfiles.OneHour, VaryByQueryKeys = ["provider"])]
    public async Task<ActionResult<List<FormFieldDefinition>>> DownloadMetadata([FromQuery] Provider provider)
    {
        var repository = serviceProvider.GetKeyedService<IContentRepository>(provider);
        if (repository == null)
            return NotFound();

        return Ok(BuildPageFormMetadata(await repository.DownloadMetadata(HttpContext.RequestAborted)));
    }

    private static List<FormFieldDefinition> BuildPageFormMetadata(List<FormFieldDefinition> formMetadata)
    {
        return
        [
            ..formMetadata,
            new FormFieldRow
            {
                ForceSingle = true,
                Controls = [
                    new TextFieldDefinition
                    {
                        Key = RequestConstants.MangaBakaKey.Key,
                    },
                    new TextFieldDefinition
                    {
                        Key = RequestConstants.MangaBakaCollectionIdKey.Key,
                    },
                ]
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.HardcoverSeriesIdKey.Key,
            },
        ];
    }

    [HttpPost("new")]
    [HttpPost("update")]
    [Authorize(Roles.ManagePages)]
    public async Task<IActionResult> UpdatePages([FromBody] PageDto dto)
    {
        await pagesService.UpdatePage(dto);

        return Ok();
    }

    [HttpPost("{pageId:guid}/set-defaults")]
    [Authorize(Roles.ManagePages)]
    public async Task<IActionResult> SetPageDefaults(Guid pageId, [FromBody] MetadataBag defaults)
    {
        await pagesService.SetPageDefaults(pageId, defaults, HttpContext.RequestAborted);

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
