using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using Flurl;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Providers.Common;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Bato;

internal class BatoRepository(ILogger<BatoRepository> logger, IDistributedCache cache, IHttpClientFactory httpClientFactory) : AbstractRepository(cache)
{
    private const string ApiPath = "/ap2/";

    private static readonly Regex CleanTitleRegex =
        new(@"[\(\[\{<«][^)\]\}>»]*[\)\]\}>»]", RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    private static readonly List<Regex> VolumeChapterRegexes =
    [
        new(@"(?:(?:Volume|Vol\.?) ?(\d+)\s+)?(?:Chapter|Ch\.?) ([\d\.]+)", RegexOptions.Compiled,
            TimeSpan.FromSeconds(5)),
        new(@"(?:\[S(\d+)] ?)?(?:Episode|Ep\.) ([\d\.]+)", RegexOptions.Compiled, TimeSpan.FromSeconds(5))
    ];

    protected override HttpClient Client => httpClientFactory.CreateClient(nameof(Provider.Bato));

    public override async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        var query = $$"""
                      {
                        "query": "query get_search_comic($select: Search_Comic_Select) { get_search_comic(select: $select) { paging { total } items { id data { name summary urlCoverOri chapterNode_up_to { data { dname } } } } } }",
                        "variables": {
                          "select": {
                            "word": "{{request.Query}}",
                            "size": {{pagination.PageSize}},
                            "page": {{pagination.PageNumber}}
                          }
                        }
                      }
                      """;

        var resp = await PostAsync(ApiPath, query, cancellationToken);

        var items = resp.SelectMany("data.get_search_comic.items[*]")
            .Select(node => new SearchResult
            {
                Id = node.SelectString("id"),
                DownloadUrl = null,
                Name = node.SelectString("data.name"),
                Provider = Provider.Bato,
                Description = node.SelectString("data.summary"),
                // Mapping dname from the nested chapter node
                Size = node.SelectString("data.chapterNode_up_to.data.dname"),
                Tags = [],
                Url = $"{Client.BaseAddress?.ToString()}title/{node.SelectString("id")}",
                ImageUrl = $"{Client.BaseAddress?.ToString().Trim('/')}{node.SelectString("data.urlCoverOri")}",
            });

        return new PagedList<SearchResult>(
            items,
            resp.SelectInt("data.get_search_comic.paging.total"),
            pagination.PageNumber,
            pagination.PageSize);
    }

    public override async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var query = $$"""
                      {
                        "query": "query get_comicNode($id: ID!) { get_comicNode(id: $id) { data { id name altNames urlCoverOri authors artists genres uploadStatus originalStatus summary } } }",
                        "variables": {
                          "id": "{{request.Id}}"
                        }
                      }
                      """;
        var chapterQuery = $$"""
                             {
                               "query": "query get_comic_chapterList($comicId: ID!, $start: Int) { get_comic_chapterList(comicId: $comicId, start: $start) { data { id dname title } } }",
                               "variables": {
                                 "comicId": "{{request.Id}}",
                                 "start": -1
                               }
                             }
                             """;


        var resp = await PostAsync(ApiPath, query, cancellationToken);
        var chapterResp = await PostAsync(ApiPath, chapterQuery, cancellationToken);

        var infoResponse = resp.Select("data.get_comicNode.data");
        var chapterResponse = chapterResp.SelectMany("data.get_comic_chapterList[*]");

        return new Series
        {
            Id = infoResponse.SelectString("id"),
            Title = infoResponse.SelectString("name"),
            LocalizedSeries = infoResponse.SelectManyString("altNames[*]").FirstOrDefault(),
            Summary = infoResponse.SelectString("summary"),
            CoverUrl = $"{Client.BaseAddress?.ToString().Trim('/')}{infoResponse.SelectString("urlCoverOri")}",
            NonProxiedCoverUrl = null,
            RefUrl = $"{Client.BaseAddress?.ToString()}title/{request.Id}",
            Status = TranslatePublicationStatus(infoResponse.SelectString("originalStatus")) ?? PublicationStatus.Unknown,
            TranslationStatus = TranslatePublicationStatus(infoResponse.SelectString("uploadStatus")),
            Year = null,
            HighestVolumeNumber = null,
            HighestChapterNumber = null,
            AgeRating = null,
            /*Tags = infoResponse.SelectManyString("genres[*]")
                .Select(g => new Tag(g))
                .ToList(),*/
            Tags = [], // We're currently not mapping the ids to actual tags
            People = infoResponse.SelectManyString("artists[*]")
                .Select(a => Person.Create(a, PersonRole.Colorist))
                .Concat(infoResponse.SelectManyString("authors[*]")
                    .Select(a => Person.Create(a, PersonRole.Writer)))
                .GroupBy(p => p.Name)
                .Select(g => new Person
                {
                    Name = g.Key,
                    Roles = g.SelectMany(p => p.Roles).Distinct().ToList(),
                })
                .ToList(),
            Links = [],
            Chapters = chapterResponse
                .Select(node => node.Select("data"))
                .Select(data =>
                {
                    var name = data.SelectString("dname");
                    var (volume, chapter) = ParseVolumeAndChapter(name);

                    return new Chapter
                    {
                        Id = data.SelectString("id"),
                        Title = data.SelectString("title"),
                        VolumeMarker = volume,
                        ChapterMarker = chapter,
                        Tags = [],
                        People = [],
                        TranslationGroups = []
                    };
                }).ToList()
        };

        PublicationStatus? TranslatePublicationStatus(string? publicationStatus)
        {
            if (string.IsNullOrEmpty(publicationStatus))
                return null;

            return publicationStatus.ToLower() switch
            {
                "pending" => PublicationStatus.Unknown,
                "ongoing" => PublicationStatus.Ongoing,
                "completed" => PublicationStatus.Completed,
                "hiatus" => PublicationStatus.Paused,
                "cancelled" => PublicationStatus.Cancelled,
                _ => PublicationStatus.Unknown
            };
        }


        (string Volume, string Chapter) ParseVolumeAndChapter(string input)
        {
            foreach (var regex in VolumeChapterRegexes)
            {
                var match = regex.Match(input);
                if (!match.Success) continue;

                var volume = match.Groups.Count > 1 ? match.Groups[1].Value : "";
                var chapter = match.Groups.Count > 2 ? match.Groups[2].Value : "";

                return (volume, chapter);
            }

            return (string.Empty, string.Empty);
        }
    }

    public override async Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        var query = $$"""
                      {
                        "query": "query get_chapterNode($id: ID!) { get_chapterNode(id: $id) { data { imageFile { urlList } } } }",
                        "variables": {
                          "id": "{{chapter.Id}}"
                        }
                      }
                      """;

        var resp = await PostAsync(ApiPath, query, cancellationToken);

        return resp.SelectManyString("data.get_chapterNode.data.imageFile.urlList[*]")
            .Select(u => new DownloadUrl(u, u))
            .ToList();
    }

    public override async Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        var result = await Client.GetCachedStringAsync("v3x", cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException("Failed to retrieve recently updated chapters", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        return document.DocumentNode.QuerySelectorAll("div.flex.border-b.border-b-base-200.pb-5")
            .Select(node =>
            {
                var contentNode = node.QuerySelector("div.group.space-y-1 a");
                var releaseNode = node.QuerySelector("span.line-clamp-1.space-x-1.grow a");

                var releaseTime = node.QuerySelector("time").GetAttributeValue("time", string.Empty);
                var releaseDate = string.IsNullOrEmpty(releaseTime)
                    ? DateTime.UtcNow
                    : DateTime.Parse(releaseTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                return new ContentRelease
                {
                    ReleaseId = releaseNode.GetAttributeValue("href", string.Empty).RemovePrefix("/title/"),
                    ReleaseName = releaseNode.QuerySelector("span")?.InnerText ?? string.Empty,
                    ContentId = contentNode.GetAttributeValue("href", string.Empty).RemovePrefix("/title/"),
                    ContentName = contentNode.QuerySelector("span")?.InnerText ?? string.Empty,
                    ReleaseDate = releaseDate.ToUniversalTime(),
                    Provider = Provider.Bato,
                };
            })
            .Where(x => !string.IsNullOrEmpty(x.ContentId))
            .Distinct()
            .ToList();
    }

    public override Task<List<FormControlDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = RequestConstants.ScanlationGroupKey,
                Advanced = true,
                Type = FormType.Text
            },
            new FormControlDefinition
            {
                Key = RequestConstants.DownloadOneShotKey,
                Type = FormType.Switch
            },
            new FormControlDefinition
            {
                Key = RequestConstants.IncludeCover,
                Type = FormType.Switch,
                DefaultOption = "true"
            },
            new FormControlDefinition
            {
                Key = RequestConstants.UpdateCover,
                Advanced = true,
                Type = FormType.Switch
            },
            new FormControlDefinition
            {
                Key = RequestConstants.TitleOverride,
                Advanced = true,
                Type = FormType.Text
            },
            new FormControlDefinition
            {
                Key = RequestConstants.AllowNonMatchingScanlationGroupKey,
                Advanced = true,
                Type = FormType.Switch,
                DefaultOption = "true"
            }
        ]);
    }

    public override Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([]);
    }
}
