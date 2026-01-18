using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface ISearchService
{
    Task<PagedList<SearchResult>> Search(SearchRequest searchRequest, PaginationParams paginationParams,
        CancellationToken cancellationToken);

    Task<List<ContentRelease>> SearchReleases(List<Provider> providers, CancellationToken cancellationToken);
}
