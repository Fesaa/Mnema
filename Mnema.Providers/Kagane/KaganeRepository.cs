using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Providers.Common;
using Mnema.Providers.Kagane.Crypto;

namespace Mnema.Providers.Kagane;

public class KaganeRepository(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache,
    ILogger<KaganeRepository> logger,
    IUnitOfWork unitOfWork): AbstractRepository(cache)
{

    private static readonly IMetadataKey<IEnumerable<string>> ContentRating = MetadataKeys.Strings("content_rating");
    private static readonly IMetadataKey<IEnumerable<string>> SourceId = MetadataKeys.Strings("source_id");
    private static readonly IMetadataKey<IEnumerable<string>> SourceType = MetadataKeys.Strings("source_type");
    private static readonly IMetadataKey<IEnumerable<string>> UploadStatus = MetadataKeys.Strings("upload_status");
    private static readonly IMetadataKey<IEnumerable<string>> PublicationStatus = MetadataKeys.Strings("publication_status");
    private static readonly IMetadataKey<IEnumerable<string>> IncludedGenres = MetadataKeys.Strings("included_genres");
    private static readonly IMetadataKey<IEnumerable<string>> ExcludedGenres = MetadataKeys.Strings("excluded_genres");
    private static readonly IMetadataKey<IEnumerable<string>> IncludedTags = MetadataKeys.Strings("included_tags");
    private static readonly IMetadataKey<IEnumerable<string>> ExcludedTags = MetadataKeys.Strings("excluded_tags");

    private AsyncLazy<List<Genre>> Genres => new(() => LoadList<Genre>("/api/v2/genres/list"));
    private AsyncLazy<List<Genre>> Tags => new(() => LoadList<Genre>("/api/v2/tags/list"));

    protected override HttpClient Client => httpClientFactory.CreateClient(nameof(Provider.Kagane));
    private string? Base64Wvd => configuration.GetSection("Authentication").GetSection("Kagane").Get<string>();

    public override async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = "/api/v2/search/series"
            .SetQueryParam("page", pagination.PageNumber)
            .SetQueryParam("size", pagination.PageSize);

        var genres = (await Genres).Select(g => g.Id).ToHashSet();
        var tags = (await Tags).Select(g => g.Id).ToHashSet();

        var body = new KaganeSearchRequest
        {
            Title = request.Query,
            Genres = new KaganaSearchRequestFilter
            {
                Values = request.GetKey(IncludedGenres).Where(genres.Contains).ToList(),
                Exclude = request.GetKey(ExcludedGenres).Where(genres.Contains).ToList(),
                MatchAll = true,
            },
            Tags = new KaganaSearchRequestFilter
            {
                Values = request.GetKey(IncludedTags).Where(tags.Contains).ToList(),
                Exclude = request.GetKey(ExcludedTags).Where(tags.Contains).ToList(),
            },
            ContentRating = request.GetKey(ContentRating).ToList(),
            SourceId = request.GetKey(SourceId).ToList(),
            SourceType = request.GetKey(SourceType).ToList(),
            UploadStatus = request.GetKey(UploadStatus).ToList(),
            PublicationStatus = request.GetKey(PublicationStatus).ToList(),
        };

        var response = await PostAsync(url.ToString(), body, cancellationToken);

        var total = response.SelectInt("total_elements");

        var items = response.SelectMany("content[*]").Select(entry => new SearchResult
        {
            Id = entry.SelectString("series_id"),
            Name = entry.SelectString("title"),
            Provider = Provider.Kagane,
            Size = null,
            Tags = [],
            Url = "https://kagane.org/series/" + entry.SelectString("series_id"),
            ImageUrl = $"proxy/kagane/covers/{entry.SelectString("cover_image_id")}",
        });

        return new PagedList<SearchResult>(items, total, pagination.PageNumber, pagination.PageSize);
    }

    public override async Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        // Kagane doesn't return which chapter caused the latest update, so there's no way for me to uniquely
        // identify updates. This is super required to not loop-download until a series is removed from recently
        // updated. We'll use the Webtoons approach of just loading series info for monitored series
        // sadly this uses more requests.

        var series = await unitOfWork.MonitoredSeriesRepository.GetByProvider(Provider.Kagane, cancellationToken);
        if (series.Count == 0) return [];

        List<ContentRelease> releases = [];

        foreach (var s in series)
        {
            var req = new DownloadRequestDto
            {
                Provider = Provider.Kagane,
                Id = s.ExternalId,
                BaseDir = s.BaseDir,
                TempTitle = s.Title,
                Metadata = s.MetadataForDownloadRequest(),
            };

            var seriesInfo = await SeriesInfo(req, cancellationToken);
            var lastChapter = seriesInfo.Chapters.LastOrDefault();

            if (lastChapter != null)
            {
                releases.Add(new ContentRelease
                {
                    ReleaseId = lastChapter.Id,
                    ReleaseName = lastChapter.Title,
                    Provider = Provider.Kagane,
                    ContentId = s.ExternalId,
                    ContentName = seriesInfo.Title,
                    ReleaseDate = lastChapter.ReleaseDate ?? DateTime.UtcNow,
                });
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }


        return releases;
    }

    public override Task<List<FormControlDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
            return Task.FromResult<List<FormControlDefinition>>([
                new FormControlDefinition
                {
                    Key = RequestConstants.ScanlationGroupKey.Key,
                    Advanced = true,
                    Type = FormType.Text
                },
                new FormControlDefinition
                {
                    Key = RequestConstants.DownloadOneShotKey.Key,
                    Type = FormType.Switch
                },
                new FormControlDefinition
                {
                    Key = RequestConstants.IncludeCover.Key,
                    Type = FormType.Switch,
                    DefaultOption = "true"
                },
                new FormControlDefinition
                {
                    Key = RequestConstants.TitleOverride.Key,
                    Advanced = true,
                    Type = FormType.Text
                },
                new FormControlDefinition
                {
                    Key = RequestConstants.AllowNonMatchingScanlationGroupKey.Key,
                    Advanced = true,
                    Type = FormType.Switch,
                    DefaultOption = "true"
                }
            ]);
    }

    public override async Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        var genres = await Genres;
        var tags = await Tags;

        return [
            new FormControlDefinition
            {
                Key = PublicationStatus.Key,
                Type = FormType.MultiSelect,
                Options = [FormControlOption.Option("Completed", "Completed"), FormControlOption.Option("Ongoing", "Ongoing"), FormControlOption.Option("Hiatus", "Hiatus"), FormControlOption.Option("Abandoned", "Abandoned")],
            },
            new FormControlDefinition
            {
                Key = ContentRating.Key,
                Type = FormType.MultiSelect,
                Options = [FormControlOption.Option("Pornographic", "Pornographic"), FormControlOption.Option("Erotica", "Erotica"), FormControlOption.Option("Suggestive", "Suggestive"), FormControlOption.Option("Safe", "Safe")],
            },
            new FormControlDefinition
            {
                Key = IncludedGenres.Key,
                Type = FormType.MultiSelect,
                Options = genres.Select(g => FormControlOption.Option(g.Name, g.Id)).ToList(),
            },
            new FormControlDefinition
            {
                Key = ExcludedGenres.Key,
                Type = FormType.MultiSelect,
                Options = genres.Select(g => FormControlOption.Option(g.Name, g.Id)).ToList(),
            },
            new FormControlDefinition
            {
                Key = IncludedTags.Key,
                Type = FormType.MultiSelect,
                Options = tags.Select(g => FormControlOption.Option(g.Name, g.Id)).ToList(),
            },
            new FormControlDefinition
            {
                Key = ExcludedTags.Key,
                Type = FormType.MultiSelect,
                Options = tags.Select(g => FormControlOption.Option(g.Name, g.Id)).ToList(),
            }
        ];
    }

    public override async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var url = "/api/v2/series/" + request.Id;

        var response = await GetAsync(url, cancellationToken);

        var coverId = response.SelectMany("series_covers[*]")
            .FirstOrDefault()?
            .SelectString("image_id");

        var genres = (await Genres).ToDictionary(g => g.Id, g => g);

        return new Series
        {
            Id = request.Id,
            Title = response.SelectString("title"),
            LocalizedSeries = response.SelectMany("series_alternate_titles[*]")
                .OrderBy(entry => entry.SelectString("label") == "ja")
                .ThenBy(entry => entry.SelectString("label") == "ja-ro")
                .ThenBy(entry => entry.SelectString("label") != "unknown")
                .FirstOrDefault()?
                .SelectString("title"),
            Summary = response.SelectString("description"),
            CoverUrl = string.IsNullOrEmpty(coverId) ? null : $"proxy/kagane/covers/{coverId}",
            NonProxiedCoverUrl = string.IsNullOrEmpty(coverId) ? null : $"https://yuzuki.kagane.org/api/v2/image/{coverId}/compressed",
            RefUrl = "https://kagane.org/series/" + request.Id,
            Status = ToPublicationStatus(response.SelectString("publication_status")),
            TranslationStatus = ToPublicationStatus(response.SelectString("upload_status")),
            Year = null,
            HighestVolumeNumber = float.TryParse(response.SelectString("total_volumes"), out var result) ? result : null,
            HighestChapterNumber = float.TryParse(response.SelectString("total_books"), out result) ? result : null,
            AgeRating = ToAgeRating(response.SelectString("content_rating")),
            Tags = response.SelectMany("genres")
                .Select(g =>
                {
                    var id = g.SelectString("genre_id");

                    if (genres.TryGetValue(id, out var value))
                    {
                        return new Mnema.Models.Publication.Tag
                        {
                            Id = id,
                            Value = value.Name,
                            IsMarkedAsGenre = value.IsActualGenre,
                        };
                    }

                    return new Mnema.Models.Publication.Tag
                    {
                        Id = id,
                        Value = g.SelectString("genre_name"),
                        IsMarkedAsGenre = false,
                    };
                })
                .Concat(response.SelectMany("tags").Select(t => new Mnema.Models.Publication.Tag
                {
                    Id = t.SelectString("tag_id"),
                    Value = t.SelectString("tag_name"),
                    IsMarkedAsGenre = false,
                }))
                .ToList(),
            People = response.SelectMany("series_staff").Select(entry =>
            {
                var name = entry.SelectString("name");
                var nativeName = entry.SelectString("native_name");
                var role = entry.SelectString("role");

                if (!string.IsNullOrEmpty(nativeName))
                    name += $" ({nativeName})";

                var personRole = role switch
                {
                    "Author" => PersonRole.Writer,
                    "Artist" => PersonRole.Colorist,
                    _ => PersonRole.Writer,
                };

                return Person.Create(name, personRole);
            }).ToList(),
            Links = [],
            Chapters = response.SelectMany("series_books[*]").Select(entry => new Chapter
            {
                Id = entry.SelectString("book_id"),
                Title = entry.SelectString("title"),
                FileName = string.Empty,
                Summary = string.Empty,
                VolumeMarker = entry.SelectString("volume_no"),
                ChapterMarker = entry.SelectString("chapter_no"),
                SortOrder = entry.SelectInt("sort_no"),
                CoverUrl = null,
                RefUrl = null,
                ReleaseDate = DateTime.TryParse(entry.SelectString("created_at"), out var time) ? time : null,
                Tags = [],
                People = [],
                TranslationGroups = response.SelectMany("groups[*]")
                    .SelectMany(g => new List<string> {
                        g.SelectString("group_id"),
                        g.SelectString("title")
                    }).ToList()
            }).ToList(),
        };
    }

    public override async Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        var wvd = Base64Wvd;
        if (string.IsNullOrEmpty(wvd)) throw new InvalidOperationException("WVD is not configured");

        var f = SHA256.HashData(Encoding.UTF8.GetBytes($":{chapter.Id}")).Take(16).ToArray();
        var challenge = GetChallengeWvd(f, wvd);

        var url = $"/api/v2/books/{chapter.Id}?data_saver=false";
        var body = JsonSerializer.Serialize(new { challenge });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = content;

        var integrityToken = await GetIntegrityToken();
        request.Headers.Add("x-integrity-token", integrityToken);

        using var response = await Client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        var accessor = new JsonAccessor(json);

        var accessToken = accessor.SelectString("access_token");
        var cacheUrl = accessor.SelectString("cache_url");

        return accessor.SelectMany("pages").Select(entry =>
        {
            var pageUrl = $"{cacheUrl}/api/v2/books/file/{chapter.Id}/{entry.SelectString("page_uuid")}"
                .SetQueryParam("token", accessToken)
                .SetQueryParam("is_datasaver", "false")
                .ToString();

            return new DownloadUrl(pageUrl, pageUrl);
        }).ToList();
    }

    private string _integrityToken = "";
    private long _integrityExpiration = 0;

    private async Task<string> GetIntegrityToken()
    {
        if (DateTimeOffset.Now.ToUnixTimeMilliseconds() < _integrityExpiration)
        {
            return _integrityToken;
        }

        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("https://kagane.org/api/integrity", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStreamAsync();
        var integrity = await JsonSerializer.DeserializeAsync<IntegrityDto>(json);
        if (integrity == null)
            throw new InvalidOperationException("Failed to get integrity token");

        _integrityToken = integrity.Token;
        _integrityExpiration = integrity.Expiration * 1000;

        return _integrityToken;
    }

    private static AgeRating ToAgeRating(string? kaganeRating)
    {
        if (string.IsNullOrEmpty(kaganeRating))
            return AgeRating.Unknown;

        switch (kaganeRating.ToLower())
        {
            case "pornographic":
                return AgeRating.AdultsOnly;
            case "erotica":
                return AgeRating.Mature17Plus;
            case "suggestive":
                return AgeRating.Teen;
            case "safe":
                return AgeRating.Everyone;
            default:
                return AgeRating.Unknown;
        }
    }

    private static PublicationStatus ToPublicationStatus(string? kaganeStatus)
    {
        if (string.IsNullOrEmpty(kaganeStatus))
            return Models.Publication.PublicationStatus.Unknown;

        switch (kaganeStatus.ToLower())
        {
            case "completed":
                return Models.Publication.PublicationStatus.Completed;
            case "ongoing":
                return Models.Publication.PublicationStatus.Ongoing;
            case "hiatus":
                return Models.Publication.PublicationStatus.Paused;
            case "abandoned":
                return Models.Publication.PublicationStatus.Cancelled;
            default:
                return Models.Publication.PublicationStatus.Unknown;
        }
    }

    private static string GetChallengeWvd(byte[] f, string wvdBase64)
    {
        var cdm = Cdm.FromData(wvdBase64);
        var psshBytes = Cdm.GetPssh(f);
        var parsed = PsshParser.Parse(psshBytes);

        if (parsed.Content is not ProtectionSystemHeaderBox pssh)
            throw new InvalidOperationException("Failed to parse PSSH box");

        return cdm.GetLicenseChallenge(pssh);
    }

    private async Task<List<T>> LoadList<T>(string url)
    {
        var res = await Client.GetCachedAsync<List<T>>(url, cache);
        if (res.IsErr)
        {
            logger.LogWarning(res.Error, "Failed to load data @ {Url}", url);
            return [];
        }

        return res.Unwrap();
    }

}
