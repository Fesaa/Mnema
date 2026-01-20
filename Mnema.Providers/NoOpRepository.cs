using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Providers;

public class NoOpRepository: IRepository
{
    public Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedList<SearchResult>.Empty());
    }

    public Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<ContentRelease>>(new List<ContentRelease>());
    }

    public Task<List<FormControlDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([]);
    }

    public Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([]);
    }

    public Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Series
        {
            Id = string.Empty,
            Title = string.Empty,
            Summary = string.Empty,
            Status = PublicationStatus.Ongoing,
            Tags = [],
            People = [],
            Links = [],
            Chapters = []
        });
    }

    public Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<DownloadUrl>>(new List<DownloadUrl>());
    }
}
