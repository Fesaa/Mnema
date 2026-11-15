using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using Flurl;
using Flurl.Util;
using Microsoft.Extensions.Caching.Distributed;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.Publication;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Repositories.Manhwaus;

public class ManhwausRepository(IHttpClientFactory httpClientFactory, IDistributedCache cache, IParserService parserService): IRepository
{

    private HttpClient HttpClient => httpClientFactory.CreateClient(nameof(Provider.Manhwaus));

    public async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = "/search/"
            .SetQueryParam("s", request.Query);

        var result = await HttpClient.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to load search results: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        var items = document.DocumentNode.QuerySelectorAll("div.page-item-detail").Select(node =>
        {
            var id = node.QuerySelector("a").GetAttributeValue("href", string.Empty);
            if (string.IsNullOrEmpty(id)) return null;


            return new SearchResult
            {
                Id = id,
                Name = node.QuerySelector("div.item-title").InnerText.Trim(),
                Provider = Provider.Manhwaus,
                ImageUrl = node.QuerySelector("a > img").GetAttributeValue("src", string.Empty),
            };
        }).WhereNotNull().ToList();

        return new PagedList<SearchResult>(items, items.Count, 0, 20);
    }

    public Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<ContentRelease>>([]);
    }

    public Task<List<FormFieldDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormFieldDefinition>>([
            new SwitchFieldDefinition
            {
                Key = RequestConstants.IncludeCover.Key,
                DefaultValue = true
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.TitleOverride.Key,
                Advanced = true
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.HardcoverSeriesIdKey.Key,
            },
            new TextFieldDefinition
            {
                Key = RequestConstants.MangaBakaKey.Key,
            }
        ]);
    }

    public Task<List<FormFieldDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<FormFieldDefinition>());
    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var url = request.Id;

        var result = await HttpClient.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve series info {request.Id}: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        var chapters = document.DocumentNode.QuerySelectorAll("ul.row-content-chapter > li").Reverse().Select((node, idx) =>
        {
            var title = node.QuerySelector("a").GetAttributeValue("title", string.Empty);
            var parsed = parserService.FullParse(title, ContentFormat.Manga);

            return new Chapter
            {
                Id = node.QuerySelector("a").GetAttributeValue("href", string.Empty),
                Title = title,
                Summary = string.Empty,
                VolumeMarker = parserService.EmptyIfLooseLeafVolume(parsed.VolumeMarker),
                ChapterMarker = parserService.EmptyIfDefaultChapter(parsed.ChapterMarker),
                SortOrder = idx,
            };
        }).ToList();

        var metadata = document.DocumentNode.QuerySelectorAll("div.summary_content > div.post-content_item").ToList();

        var pubStatus = document.DocumentNode.QuerySelector("div.minfo > i").InnerText
            .ParseEnumMemberValueOrDefault(PublicationStatus.Ongoing);

        return new Series
        {
            Id = request.Id,
            Title = document.DocumentNode.QuerySelector("div.post-title").InnerText.Trim(),
            LocalizedSeries = metadata.ElementAtOrDefault(1)?.QuerySelector("span")?.InnerText.Split(',').LastOrDefault(),
            Summary = metadata.ElementAtOrDefault(2)?.QuerySelector("div.entry-content")?.InnerText.Trim() ?? string.Empty,
            OriginalLanguage = string.Empty,
            CoverUrl = document.DocumentNode.QuerySelector("div.summary_image img")?.GetAttributeValue("src", string.Empty),
            RefUrl = $"{HttpClient.BaseAddress}{request.Id}",
            Status = pubStatus,
            HighestVolumeNumber = pubStatus == PublicationStatus.Completed ? chapters.Last().VolumeNumber() : null,
            HighestChapterNumber = pubStatus == PublicationStatus.Completed ? chapters.Last().ChapterNumber() : null,
            Tags = metadata.LastOrDefault()?.QuerySelectorAll("a").Select(node => new Tag(node.InnerText.Trim(), true)).ToList() ?? [],
            People = metadata.Skip(3).TakeWhile(node => node.QuerySelector("b") is not null).Select(node =>
            {
                var type = node.QuerySelector("b").InnerText.Trim();
                var name = node.QuerySelector("a").InnerText.Trim();

                if (type.Contains("Author"))
                {
                    return Person.Create(name, PersonRole.Writer);
                }

                if (type.Contains("Artist"))
                {
                    return Person.Create(name, PersonRole.Colorist);
                }

                return null;
            }).WhereNotNull().ToList(),
            Links = [],
            Chapters = chapters,
            ContentFormat = ContentFormat.Manga
        };
    }

    public async Task<IList<DownloadUrl>> ChapterUrls(MetadataBag metadata, Chapter chapter, CancellationToken cancellationToken)
    {
        var result = await HttpClient.GetCachedStringAsync(chapter.Id, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve chapter urls for {chapter.Id}: {result.Error?.Message}", result.Error);

        return result.Unwrap().ToHtmlDocument()
            .DocumentNode
            .QuerySelectorAll("div.read-content > img")
            .Select(node => node.GetAttributeValue("src", null))
            .WhereNotNull()
            .Select(url => new DownloadUrl(url, url))
            .ToList();
    }
}
