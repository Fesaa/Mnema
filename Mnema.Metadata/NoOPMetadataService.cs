using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.External;
using Mnema.Models.Publication;

namespace Mnema.Metadata;

public class NoOpMetadataService: IMetadataProviderService
{
    public Task<PagedList<MetadataSearchResult>> Search(MetadataSearchDto search, PaginationParams paginationParams, CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedList<MetadataSearchResult>.Empty());
    }

    public Task<Series?> GetSeries(string externalId, CancellationToken cancellationToken)
    {
        return Task.FromResult<Series?>(null);
    }

    public Task<List<Cover>> GetCovers(string externalId, CancellationToken cancellationToken)
    {
        return Task.FromResult<List<Cover>>([]);
    }
}
