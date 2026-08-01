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
using Mnema.Models.Enums;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Repositories.Nyaa;

public class NyaaRepository(
    IHttpClientFactory httpClientFactory, IGroupedReleaseDetector groupedReleaseDetector,
    IScannerService scannerService, IParserService parserService
    ): IContentRepository
{

    private static readonly XmlSerializer XmlSerializer = new(typeof(RssFeed));
    private const string DateTimeFormat = "ddd, dd MMM yyyy HH:mm:ss '-0000'";
    private static readonly IMetadataKey<string> Category = MetadataKeys.String("category", "3_1");
    private static readonly IMetadataKey<string> Filter = MetadataKeys.String("filter", "0");

    private HttpClient HttpClient => httpClientFactory.CreateClient(nameof(Provider.Nyaa));

    public async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = "/"
            .SetQueryParam("page", "rss")
            .SetQueryParam("c", request.GetKey(Category))
            .SetQueryParam("f", request.GetKey(Filter))
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

        List<ContentRelease> releases = [];

        foreach (var item in feed.Channel.Items)
        {
            if (!groupedReleaseDetector.IsGroupedRelease(Provider.Nyaa, item.Title))
            {
                releases.Add(new ContentRelease
                {
                    ReleaseId = item.InfoHash,
                    ReleaseName = item.Title,
                    ReleaseDate = item.PubDate.AsDateTime(DateTimeFormat) ?? DateTime.UtcNow,
                    DownloadUrl =  item.Link,
                    Provider = Provider.Nyaa,
                });
                continue;
            }

            var torrentInfo = await scannerService.ParseTorrentFile(item.Link, cancellationToken);

            releases.AddRange(torrentInfo.Files
                .Select(f =>
                {
                    var contentFormat = f.FileName.GetFileType().ContentFormatFromFileExt();
                    return contentFormat == null ? null : parserService.FullParse(f.FileName, contentFormat.Value);
                })
                .WhereNotNull()
                .GroupMergingSeries()
                .Select(g =>
                {
                    if (g.Items.Count == 0) return null;

                    var lastestResult = g.Items.MaxBy(i => i.Chapter.MaxNumber);
                    if (lastestResult == null) return null;

                    return new ContentRelease
                    {
                        ReleaseId = $"{item.InfoHash}#{g.Series.First().ToNormalized()}",
                        ContentId = item.InfoHash,
                        ReleaseName = g.Series.First(),
                        ContentName = lastestResult.Input,
                        ReleaseDate = item.PubDate.AsDateTime(DateTimeFormat) ?? DateTime.UtcNow,
                        DownloadUrl =  item.Link,
                        Provider = Provider.Nyaa,
                        IsGroupedRelease = true,
                    };
                })
                .WhereNotNull());
        }

        return releases;
    }

    public Task<List<FormFieldDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormFieldDefinition>>([
            new DropDownFieldDefinition<Format>(FieldValueType.Integer)
            {
                Key = RequestConstants.FormatKey.Key,
                Options = Enum.GetValues<Format>()
                    .Select(f => new SelectOption<Format>(f.ToString().ToLower(), f))
                    .ToList(),
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .Build(),
                DefaultValue = Format.Archive,
            },
            new DropDownFieldDefinition<ContentFormat>(FieldValueType.Integer)
            {
                Key = RequestConstants.ContentFormatKey.Key,
                Options = Enum.GetValues<ContentFormat>()
                    .Select(f => new SelectOption<ContentFormat>(f.ToString().ToLower(), f))
                    .ToList(),
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .Build(),
                DefaultValue = ContentFormat.Manga
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.HardcoverSeriesIdKey.Key,
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.MangaBakaKey.Key,
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.TitleOverride.Key,
            },
            new SwitchFieldDefinition
            {
                Key = RequestConstants.IncludeCover.Key,
                DefaultValue = false,
                Advanced = true,
            },
            new SwitchFieldDefinition
            {
                Key = RequestConstants.IgnoreNonMatchedVolumes.Key,
                DefaultValue = true,
                Advanced = true,
            },
        ]);
    }

    public Task<List<FormFieldDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormFieldDefinition>>([
            new DropDownFieldDefinition<string>
            {
                Key = "category",
                Options = [
                    new SelectOption<string>("All", "0"),
                    new SelectOption<string>("Anime", "1_0"),
                    new SelectOption<string>("Anime - AMV", "1_1"),
                    new SelectOption<string>("Anime - English Translated", "1_2"),
                    new SelectOption<string>("Anime - Non English Translated", "1_3"),
                    new SelectOption<string>("Anime - Raw", "1_4"),
                    SelectOption<string>.DefaultOption("Literature", "3_0"),
                    new SelectOption<string>("Literature - English Translated", "3_1"),
                    new SelectOption<string>("Literature - Non English Translated", "3_2"),
                    new SelectOption<string>("Literature - Raw", "3_3"),
                ],
            },
            new DropDownFieldDefinition<string>
            {
                Key = "filter",
                Options = [
                    SelectOption<string>.DefaultOption("No Filter", "0"),
                    new SelectOption<string>("No Remakes", "1"),
                    new SelectOption<string>("Only Trusted", "2"),
                ],
            },
        ]);
    }
}
