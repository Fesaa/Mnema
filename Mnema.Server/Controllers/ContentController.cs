using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Providers;

namespace Mnema.Server.Controllers;

public class ContentController(ISearchService searchService, IDownloadService downloadService, IServiceProvider serviceProvider): BaseApiController
{

    [AllowAnonymous]
    [HttpPost("search")]
    public Task<PagedList<SearchResult>> Search(SearchRequest searchRequest, [FromQuery] PaginationParams? pagination)
    {
        pagination ??= PaginationParams.Default;

        return searchService.Search(searchRequest, pagination, HttpContext.RequestAborted);
    }

    [AllowAnonymous]
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
            DownloadMetadata = new DownloadMetadataDto(),
        };
        
        return Ok(await repository.SeriesInfo(request, HttpContext.RequestAborted));
    }
    
    [AllowAnonymous]
    [HttpGet("chapter-urls")]
    public async Task<ActionResult<List<DownloadUrl>>> GetChapterUrls([FromQuery] Provider provider, [FromQuery] string id)
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
        return Ok(await downloadService.GetCurrentContent(Guid.Parse("2f461b21-85f0-4e1e-b64b-fb48c774cdb6")));
    }
    
    [AllowAnonymous]
    [HttpPost("stop")]
    public async Task<IActionResult> Stop(StopRequestDto request)
    {
        request.UserId = Guid.Parse("2f461b21-85f0-4e1e-b64b-fb48c774cdb6");

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