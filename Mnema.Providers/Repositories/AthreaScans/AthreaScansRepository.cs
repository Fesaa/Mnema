using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Distributed;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Repositories.AthreaScans;

public class AthreaScansRepository(IHttpClientFactory httpClientFactory, IDistributedCache cache): IRepository
{

    private HttpClient HttpClient => httpClientFactory.CreateClient(nameof(Provider.AthreaScans));

    public async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = $"/page/{pagination.PageNumber}/?s={request.Query}";

        var result = await HttpClient.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to load search results: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        var results = document.DocumentNode.QuerySelectorAll(".listupd > div.bs") ?? [];
        var paginator = document.DocumentNode.QuerySelectorAll("a.page-numbers").ToList();

        var totalPages = paginator.Count > 1 ? paginator[^2].InnerText.AsInt() : 1;

        var items = results.Select(node =>
        {
            var id = GetIdFromAnchor(node.QuerySelector("a"));

            var title = node.QuerySelector("a")?.GetAttributeValue("title", string.Empty);

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(title)) return null;

            return new SearchResult
            {
                Id = id,
                Name = title,
                ImageUrl = node.QuerySelector("img")?.GetAttributeValue("src", string.Empty) ?? string.Empty,
                Url = node.QuerySelector("a")?.GetAttributeValue("href", string.Empty),
                Provider = Provider.AthreaScans
            };
        }).WhereNotNull().ToList();

        return new PagedList<SearchResult>(items, totalPages * 10, pagination.PageNumber, 10);
    }

    public async Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        var result = await HttpClient.GetCachedStringAsync("/", cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to load recently updated: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        var series = document.DocumentNode.QuerySelectorAll(".listupd > div.bs > div > div.bigor") ?? [];

        return series.Select(node =>
        {
            var seriesId = GetIdFromAnchor(node.QuerySelector("div.tt > a"));

            if (string.IsNullOrEmpty(seriesId)) return null;

            var title = node.QuerySelector("div.tt > a")?.InnerText;

            var lastAvailableChapter = node
                .QuerySelectorAll("li")
                .FirstOrDefault(n => n.QuerySelector(".fas.fa-coins") == null);

            if (lastAvailableChapter == null) return null;

            var chapterId = GetIdFromAnchor(lastAvailableChapter.QuerySelector("a"));

            if (string.IsNullOrEmpty(chapterId)) return null;

            return new ContentRelease
            {
                Provider = Provider.AthreaScans,
                ReleaseId = chapterId,
                ReleaseName = lastAvailableChapter.InnerText,
                ContentId = seriesId,
                ContentName = title ?? string.Empty,
                ReleaseDate = DateTime.UtcNow,
            };
        }).WhereNotNull().ToList();
    }

    public Task<List<FormControlDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = RequestConstants.IncludeCover.Key,
                Type = FormType.Switch,
                DefaultOption = "true"
            },
            new FormControlDefinition
            {
                Key = RequestConstants.TitleOverride.Key,
                Type = FormType.Text,
                Advanced = true
            },
            new FormControlDefinition
            {
                Key = RequestConstants.HardcoverSeriesIdKey.Key,
                Type = FormType.Text
            },
            new FormControlDefinition
            {
                Key = RequestConstants.MangaBakaKey.Key,
                Type = FormType.Text
            }
        ]);
    }

    public Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([]);
    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var url = "/manga/" + request.Id;

        var result = await HttpClient.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve series info {request.Id}: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        var chapters = document.DocumentNode.QuerySelectorAll("li > div.chbox")
            .Where(n => n.QuerySelector(".text-gold") == null)
            .Select(node => {
                var chapterId = GetIdFromAnchor(node.QuerySelector("a"));
                var chapterMarker = node.QuerySelector(".chapternum")?.InnerText?
                    ["Chapter".Length..]
                    .Trim();
                if (string.IsNullOrEmpty(chapterId) || string.IsNullOrEmpty(chapterMarker)) return null;

                return new Chapter
                {
                    Id = chapterId,
                    Title = string.Empty,
                    VolumeMarker = string.Empty,
                    ChapterMarker = chapterMarker,
                    Tags = [],
                    People = [],
                    TranslationGroups = []
                };
            }).WhereNotNull().ToList();

        var infoTableRows = document.DocumentNode.QuerySelectorAll("table.infotable tr")
            .Select(node =>
            {
                var columns = node.QuerySelectorAll("td").ToList();
                var key = columns.ElementAtOrDefault(0)?.InnerText.ToLower();
                var value = columns.ElementAtOrDefault(1)?.InnerText;

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) return null;

                return (KeyValuePair<string, string>?) new KeyValuePair<string, string>(key, value);
            })
            .WhereNotNull()
            .ToDictionary(kv => kv!.Value.Key, kv => kv!.Value.Value);

        var author = infoTableRows.TryGetValue("author", out var authorName) ? Person.Create(authorName, PersonRole.Writer) : null;
        var artist = infoTableRows.TryGetValue("artist", out var artistName) ? Person.Create(artistName, PersonRole.Colorist) : null;
        List<Person?> people = [author, artist];

        return new Series
        {
            Id = request.Id,
            Title = document.DocumentNode.QuerySelector(".entry-title")?.InnerText ?? string.Empty,
            Summary = document.DocumentNode.QuerySelector(".entry-content.entry-content-single")?.InnerText ?? string.Empty,
            LocalizedSeries = infoTableRows.TryGetValue("alternative", out var seriesName) ? seriesName : string.Empty,
            CoverUrl = document.DocumentNode.QuerySelector("seriestucontl > img")?.GetAttributeValue("src", string.Empty),
            Status = PublicationStatus.Unknown,
            Tags = document.DocumentNode.QuerySelectorAll("div.seriestugenre > a")
                .Select(node => new Tag(node.InnerText, true)).ToList(),
            People = people.WhereNotNull().ToList(),
            Links = [],
            Chapters = chapters
        };
    }

    public async Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        var result = await HttpClient.GetCachedStringAsync(chapter.Id, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve chapter urls for {chapter.Id}: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        var reader = document.DocumentNode.SelectSingleNode("//div[@id='readerarea']");

        return reader.QuerySelectorAll("img")
            .Select(node => node.GetAttributeValue("src", string.Empty))
            .Where(url => !string.IsNullOrEmpty(url))
            .Select(s => new DownloadUrl(s, s))
            .ToList();
    }

    private static string? GetIdFromAnchor(HtmlNode? node)
    {
        return node?
            .GetAttributeValue("href", string.Empty)
            .Trim('/').Split('/')
            .LastOrDefault();
    }
}
