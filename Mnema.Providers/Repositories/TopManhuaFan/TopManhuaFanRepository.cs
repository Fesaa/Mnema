using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using Flurl;
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

namespace Mnema.Providers.Repositories;

public class TopManhuaFanRepository(IHttpClientFactory httpClientFactory, IDistributedCache cache, IParserService parserService): IRepository
{

    private HttpClient HttpClient => httpClientFactory.CreateClient(nameof(Provider.TopManhuaFan));

    public async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = string.Empty
            .SetQueryParam("s", request.Query)
            .SetQueryParam("post_type", "wp-manga");

        var result = await HttpClient.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to load search results: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        var items = document.DocumentNode.QuerySelectorAll(".c-tabs-item__content")
            .Select(node =>
            {
                var titleNode = node.QuerySelector(".tab-summary .post-title a");

                var id = titleNode.GetAttributeValue("href", string.Empty)
                    .RemovePrefix(HttpClient.BaseAddress?.ToString() ?? string.Empty)
                    .TrimStart('/');

                if (string.IsNullOrEmpty(id)) return null;

                return new SearchResult
                {
                    Id = id,
                    Name = titleNode.InnerText,
                    Url = titleNode.GetAttributeValue("href", string.Empty),
                    // Requires proxy can't be bothered
                    //ImageUrl = node.QuerySelector(".tab-thumb img").GetAttributeValue("src", string.Empty),
                    Provider = Provider.TopManhuaFan
                };
            }).WhereNotNull().ToList();

        if (items.Count == 0)
            return PagedList<SearchResult>.Empty();

        return new PagedList<SearchResult>(items, items.Count, 0, items.Count);
    }

    public async Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        var result = await HttpClient.GetCachedStringAsync(string.Empty, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to load recently updated: {result.Error?.Message}", result.Error);

        return result.Unwrap().ToHtmlDocument()
            .DocumentNode
            .QuerySelectorAll(".c-page .page-item-detail")
            .Select(node =>
            {
                var seriesId = node.QuerySelector("a").GetAttributeValue("href", string.Empty).TrimStart('/');
                if (string.IsNullOrEmpty(seriesId)) return null;

                var title = node.QuerySelector(".post-title a")?.InnerText;

                var lastAvailableChapter = node
                    .QuerySelectorAll(".list-chapter .chapter a")
                    .FirstOrDefault();

                if (lastAvailableChapter == null) return null;

                var chapterId = lastAvailableChapter.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrEmpty(chapterId)) return null;

                return new ContentRelease
                {
                    Provider = Provider.TopManhuaFan,
                    ReleaseId = chapterId,
                    ReleaseName = lastAvailableChapter.InnerText.Trim(),
                    ContentId = seriesId,
                    ContentName = title?.Trim() ?? string.Empty,
                    ReleaseDate = DateTime.UtcNow,
                };
            })
            .WhereNotNull()
            .ToList();
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
        return Task.FromResult<List<FormFieldDefinition>>([]);
    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var url = request.Id;

        var result = await HttpClient.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve series info {request.Id}: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        var chapters = document.DocumentNode.QuerySelectorAll(".chapter-list .wp-manga-chapter")
            .Select(node =>
            {
                var titleNode = node.QuerySelector("a");
                var id = titleNode.GetAttributeValue("href", string.Empty)
                    .RemovePrefix(HttpClient.BaseAddress?.ToString() ?? string.Empty)
                    .TrimStart('/');

                if (string.IsNullOrEmpty(id)) return null;

                var title = string.Empty;
                var toParse = titleNode.InnerText.Split(':').First();
                if (titleNode.InnerText.Contains(':'))
                    title = titleNode.InnerText.Split(':').Last();

                var parseResult = parserService.FullParse(toParse, ContentFormat.Manga);

                return new Chapter
                {
                    Id = id,
                    Title = title,
                    VolumeMarker = parserService.EmptyIfLooseLeafVolume(parseResult.VolumeMarker),
                    ChapterMarker = parserService.EmptyIfDefaultChapter(parseResult.ChapterMarker),
                };
            })
            .WhereNotNull()
            .ToList();

        var summary = document.DocumentNode
            .QuerySelectorAll(".description-summary .summary__content p")
            .Skip(1)
            .FirstOrDefault()?
            .InnerText
            .Trim();

        return new Series
        {
            Id = request.Id,
            Title = document.DocumentNode.QuerySelector(".post-title h1").InnerText,
            Summary = summary ?? string.Empty,
            Status = PublicationStatus.Unknown,
            Tags = document.DocumentNode.QuerySelectorAll(".summary-content .genres-content a")
                .Select(node => node.InnerText.Trim(' '))
                // Not marking as genre as it's not the best metadata
                .Select(genre => new Tag(genre))
                .ToList(),
            People = [],
            Links = [],
            Chapters = chapters
        };
    }

    public async Task<IList<DownloadUrl>> ChapterUrls(MetadataBag metadata, Chapter chapter, CancellationToken cancellationToken)
    {
        var result = await HttpClient.GetCachedStringAsync(chapter.Id, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve chapter urls for {chapter.Id}: {result.Error?.Message}", result.Error);

        return result.Unwrap().ToHtmlDocument()
            .DocumentNode
            .QuerySelectorAll(".wp-manga-chapter-img")
            .Select(node => node.GetAttributeValue("src", null))
            .WhereNotNull()
            .Select(url => new DownloadUrl(url, url, url.GetFileType()))
            .ToList();
    }
}
