using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Mangadex;

public class MangadexRepository(ILogger<MangadexRepository> logger, IDistributedCache cache, IHttpClientFactory httpClientFactory): IRepository
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new ()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly DistributedCacheEntryOptions _cacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };

    public async Task<PagedList<SearchResult>> SearchPublications(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = "/manga".SetQueryParam("title", request.Query)
            .AddRange("status", request.Modifiers.GetStrings("status"))
            .AddRange("contentRating", request.Modifiers.GetStrings("contentRating"))
            .AddRange("publicationDemographic", request.Modifiers.GetStrings("publicationDemographic"))
            .SetQueryParam("includes[]", "cover_art")
            .AppendQueryParam("includes[]", "author")
            .AppendQueryParam("includes[]", "artist")
            .SetQueryParam("offset", pagination.PageNumber * pagination.PageSize)
            .SetQueryParam("limit", pagination.PageSize);
        
        var result = await GetCachedAsync<MangadexSearchResponse>(url.ToString(), cancellationToken);
        if (result.IsErr)
        {
            throw new MnemaException("Failed to search for series", result.Error);
        }

        var response = result.Unwrap();
        if (response.Data == null)
        {
            logger.LogError("Response contained null data, did something go wrong?");
            return PagedList<SearchResult>.Empty();
        }
        
        logger.LogDebug("Found {Amount} items out of {Total} for query {Query}", response.Data.Count, response.Total, request.Query);

        var items = response.Data.Select(searchResult => new SearchResult
        {
            Id = searchResult.Id,
            Name = searchResult.Attributes.LangTitle("en"),
            Provider = Provider.Mangadex,
            Description = searchResult.Attributes.Description.GetValueOrDefault("en"),
            Size = searchResult.Attributes.Size(),
            Tags = [],
            Url = searchResult.RefUrl,
            ImageUrl = searchResult.CoverUrl(),
        });

        return new PagedList<SearchResult>(items, response.Total, response.Offset, response.Limit);
    }

    public Task<Series> SeriesInfo(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<Result<TResult, HttpRequestException>> GetCachedAsync<TResult>(string url, CancellationToken cancellationToken = default)
    {
        var cachedResponse = await cache.GetAsJsonAsync<TResult>(url, cancellationToken);
        if (cachedResponse != null)
        {
            return Result<TResult, HttpRequestException>.Ok(cachedResponse);
        }
        
        var client = httpClientFactory.CreateClient(nameof(Provider.Mangadex));

        var result = await client.GetAsync<TResult>(url, JsonSerializerOptions, cancellationToken);
        if (result.IsErr)
        {
            return result;
        }

        var resultValue = result.Unwrap();
        if (resultValue != null)
        {
            await cache.SetAsJsonAsync(url, result.Unwrap(), _cacheEntryOptions, cancellationToken);
        }

        return result;
    }

}