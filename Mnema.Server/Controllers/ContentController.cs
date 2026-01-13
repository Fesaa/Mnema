using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Server.Controllers;

public class ContentController(
    ISearchService searchService,
    IDownloadService downloadService,
    IServiceProvider serviceProvider) : BaseApiController
{
    [HttpPost("search")]
    public Task<PagedList<SearchResult>> Search(SearchRequest searchRequest, [FromQuery] PaginationParams? pagination)
    {
        pagination ??= PaginationParams.Default;

        return searchService.Search(searchRequest, pagination, HttpContext.RequestAborted);
    }

    [HttpGet("recently-updated")]
    public async Task<ActionResult<IList<ContentRelease>>> GetRecentlyUpdated([FromQuery] Provider provider)
    {
        var repository = serviceProvider.GetKeyedService<IContentRepository>(provider);
        if (repository == null)
            return NotFound();

        return Ok(await repository.GetRecentlyUpdated(HttpContext.RequestAborted));
    }

    [HttpGet("series-info")]
    public async Task<ActionResult<Series>> GetSeriesInfo([FromQuery] Provider provider, [FromQuery] string id)
    {
        var repository = serviceProvider.GetKeyedService<IRepository>(provider);
        if (repository == null)
            return NotFound();

        var request = new DownloadRequestDto
        {
            Provider = provider,
            Id = id,
            BaseDir = string.Empty,
            TempTitle = string.Empty,
            Metadata = new MetadataBag(),
        };

        return Ok(await repository.SeriesInfo(request, HttpContext.RequestAborted));
    }

    [HttpGet("chapter-urls")]
    public async Task<ActionResult<List<DownloadUrl>>> GetChapterUrls([FromQuery] Provider provider,
        [FromQuery] string id)
    {
        var repository = serviceProvider.GetKeyedService<IRepository>(provider);
        if (repository == null)
            return NotFound();

        var chapter = new Chapter
        {
            Id = id,
            Title = string.Empty,
            VolumeMarker = string.Empty,
            ChapterMarker = string.Empty,
            Tags = [],
            People = [],
            TranslationGroups = []
        };

        return Ok(await repository.ChapterUrls(chapter, HttpContext.RequestAborted));
    }

    [HttpPost("download")]
    public async Task<IActionResult> Download(DownloadRequestDto request)
    {
        request.UserId = UserId;

        await downloadService.StartDownload(request);

        return Ok();
    }

    [HttpGet("form")]
    public ActionResult<FormDefinition> GetForm()
    {
        return Ok(new FormDefinition
        {
            Key = "download-modal",
            Controls = [
                new FormControlDefinition
                {
                    Key = "dir",
                    Field = "baseDir",
                    Type = FormType.Directory,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = "start-immediately",
                    Field = "startImmediately",
                    Type = FormType.Switch,
                    DefaultOption = true,
                }
            ]
        });
    }

    [HttpGet("stats")]
    public async Task<ActionResult<IEnumerable<DownloadInfo>>> Stats()
    {
        return Ok(await downloadService.GetCurrentContent(UserId));
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop(StopRequestDto request)
    {
        request.UserId = UserId;

        await downloadService.CancelDownload(request);

        return Ok();
    }

    [HttpPost("message")]
    public async Task<ActionResult<MessageDto>> RelayMessage(MessageDto message)
    {
        var contentManager = serviceProvider.GetKeyedService<IContentManager>(message.Provider);
        if (contentManager == null)
            return NotFound();

        return Ok(await contentManager.RelayMessage(message));
    }
}
