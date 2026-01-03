using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.Content;

namespace Mnema.API;

public interface ISearchService
{
    Task<PagedList<SearchResult>> Search(SearchRequest searchRequest, PaginationParams paginationParams, CancellationToken cancellationToken);
}