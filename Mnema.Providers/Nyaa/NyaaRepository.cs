using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Flurl;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Common.Helpers;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;

namespace Mnema.Providers.Nyaa;

public class NyaaRepository(IHttpClientFactory httpClientFactory): IContentRepository
{

    private static readonly XmlSerializer XmlSerializer = new(typeof(RssFeed));
    private const string DateTimeFormat = "ddd, dd MMM yyyy HH:mm:ss '-0000'";

    private HttpClient HttpClient => httpClientFactory.CreateClient(nameof(Provider.Nyaa));

    public async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = "/"
            .SetQueryParam("page", "rss")
            .SetQueryParam("c", "3_1")
            .SetQueryParam("f", "0")
            .SetQueryParam("q", request.Query);

        var stream = await HttpClient.GetStreamAsync(url, cancellationToken);

        var feed = XmlHelper.Deserialize<RssFeed>(XmlSerializer, stream);
        if (feed == null)
        {
            return PagedList<SearchResult>.Empty();
        }

        var items = feed.Channel.Items.Select(item => new SearchResult
        {
            Id = item.InfoHash,
            Name = item.Title,
            Description = item.Description,
            Size = item.Size,
            DownloadUrl = item.Link,
            Url = item.Guid.Value,
            Tags = [
                item.Category
            ],
            Provider = Provider.Nyaa
        }).ToList();

        return new PagedList<SearchResult>(items, items.Count, 1, items.Count);
    }

    public async Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        var url = "/"
            .SetQueryParam("page", "rss")
            .SetQueryParam("c", "3_1")
            .SetQueryParam("f", "0");

        var stream = await HttpClient.GetStreamAsync(url, cancellationToken);

        var feed = XmlHelper.Deserialize<RssFeed>(XmlSerializer, stream);
        if (feed == null)
        {
            return [];
        }

        return feed.Channel.Items
            .Select(item => new ContentRelease
            {
                ReleaseId = item.InfoHash,
                ReleaseName = item.Title,
                ReleaseDate = item.PubDate.AsDateTime(DateTimeFormat) ?? DateTime.UtcNow,
            })
            .ToList();
    }

    public Task<List<FormControlDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
        ]);
    }

    public Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = RequestConstants.FormatKey,
                Type = FormType.DropDown,
                ValueType = FormValueType.Integer,
                Options = Enum.GetValues<Format>()
                    .Select(f => new FormControlOption(f.ToString().ToLower(), f))
                    .ToList(),
            },
            new FormControlDefinition
            {
                Key = RequestConstants.ContentFormatKey,
                Type = FormType.DropDown,
                ValueType = FormValueType.Integer,
                Options = Enum.GetValues<ContentFormat>()
                    .Select(f => new FormControlOption(f.ToString().ToLower(), f))
                    .ToList(),
            },
            new FormControlDefinition
            {
                Key = RequestConstants.HardcoverSeriesIdKey,
                Type = FormType.Text,
                ValueType = FormValueType.Integer,
            }
        ]);
    }
}
