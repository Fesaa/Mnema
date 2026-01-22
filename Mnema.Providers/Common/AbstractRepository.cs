using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Providers.Common;

public abstract class AbstractRepository(IDistributedCache cache): IRepository
{
    protected abstract HttpClient Client { get; }

    public async Task<JsonAccessor> GetAsync(string url, CancellationToken cancellationToken)
    {
        var response = await Client.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);
        if (response.IsErr)
            throw new MnemaException("Failed to retrieve data", response.Error);

        return new JsonAccessor(response.Unwrap());
    }

    public async Task<JsonAccessor> PostAsync(string url, string json, CancellationToken cancellationToken)
    {
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostResultASync(url, content, cancellationToken: cancellationToken);
        if (response.IsErr)
            throw new MnemaException("Failed to retrieve data", response.Error);

        return new JsonAccessor(response.Unwrap());
    }

    public abstract Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination,
        CancellationToken cancellationToken);

    public abstract Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken);

    public abstract Task<List<FormControlDefinition>> DownloadMetadata(CancellationToken cancellationToken);

    public abstract Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken);

    public abstract Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken);

    public abstract Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken);
}
