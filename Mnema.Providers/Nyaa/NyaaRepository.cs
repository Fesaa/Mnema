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
            .SetQueryParam("c", request.Modifiers.GetStringOrDefault("category", "3_1"))
            .SetQueryParam("f", request.Modifiers.GetStringOrDefault("filter", "0"))
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
                DownloadUrl =  item.Link,
                Provider = Provider.Nyaa,
            })
            .ToList();
    }

    public Task<List<FormControlDefinition>> DownloadMetadata(CancellationToken cancellationToken)
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
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .Build(),
                DefaultOption = Format.Archive,
            },
            new FormControlDefinition
            {
                Key = RequestConstants.ContentFormatKey,
                Type = FormType.DropDown,
                ValueType = FormValueType.Integer,
                Options = Enum.GetValues<ContentFormat>()
                    .Select(f => new FormControlOption(f.ToString().ToLower(), f))
                    .ToList(),
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .Build(),
                DefaultOption = ContentFormat.Manga
            },
            new FormControlDefinition
            {
                Key = RequestConstants.HardcoverSeriesIdKey,
                Type = FormType.Text,
                ValueType = FormValueType.Integer,
            },
            new FormControlDefinition
            {
                Key = RequestConstants.MangaBakaKey,
                Type = FormType.Text,
                ValueType = FormValueType.Integer,
            },
            new FormControlDefinition
            {
                Key = RequestConstants.TitleOverride,
                Type = FormType.Text
            },
        ]);
    }

    public Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = "category",
                Type = FormType.DropDown,
                Options = [
                    new FormControlOption("All", "0"),
                    new FormControlOption("Anime", "1_0"),
                    new FormControlOption("Anime - AMV", "1_1"),
                    new FormControlOption("Anime - English Translated", "1_2"),
                    new FormControlOption("Anime - Non English Translated", "1_3"),
                    new FormControlOption("Anime - Raw", "1_4"),
                    FormControlOption.DefaultValue("Literature", "3_0"),
                    new FormControlOption("Literature - English Translated", "3_1"),
                    new FormControlOption("Literature - Non English Translated", "3_2"),
                    new FormControlOption("Literature - Raw", "3_3"),
                ],
            },
            new FormControlDefinition
            {
                Key = "filter",
                Type = FormType.DropDown,
                Options = [
                    FormControlOption.DefaultValue("No Filter", "0"),
                    new FormControlOption("No Remakes", "1"),
                    new FormControlOption("Only Trusted", "2"),
                ],
            },
        ]);
    }
}
