using System.Diagnostics;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.External;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Metadata.Mangabaka;

internal partial class MangabakaMetadataService(
    ILogger<MangabakaMetadataService> logger,
    IUnitOfWork unitOfWork,
    MangabakaDbContext ctx,
    [FromKeyedServices(key: MetadataProvider.Mangabaka)] SearcherManager searcherManager,
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache
): IMetadataProviderService
{

    private static readonly HashSet<string> StopWords = [..EnglishAnalyzer.DefaultStopSet];

    private HttpClient HttpClient => httpClientFactory.CreateClient(nameof(MetadataProvider.Mangabaka));

    public async Task<PagedList<MetadataSearchResult>> Search(MetadataSearchDto search, PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var searcher = searcherManager.Acquire();
        try
        {
            var parser = new MultiFieldQueryParser(MangabakaScheduler.Version, MangabakaFields.TitleFields, MangabakaFields.PerFieldAnalyzer())
            {
                AllowLeadingWildcard = true,
                DefaultOperator = Operator.OR,
            };

            var isCjk = search.Query.Any(c => c >= 0x3000 && c <= 0xD7A3);
            Query query;
            if (isCjk)
            {
                query = parser.Parse(QueryParserBase.Escape(search.Query.Trim()));
            }
            else
            {
                var trimmed = search.Query.Trim().ToLowerInvariant();
                var normalized = TermNormalisationRegex().Replace(trimmed, " ");

                var terms = normalized
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => t.Length > 1 && !StopWords.Contains(t))
                    .Select(QueryParserBase.Escape)
                    .ToList();

                query = parser.Parse(string.Join(" ", terms.Select(t => $"+{t}*")));
            }

            var hitsToFetch = (paginationParams.PageNumber + 1) * paginationParams.PageSize;
            var topDocs = searcher.Search(query, hitsToFetch);

            logger.LogTrace("Search took {Elapsed}ms, found {Total} items out of {Requested} requested", sw.ElapsedMilliseconds,
                topDocs.TotalHits, hitsToFetch);

            var scoreDocs = topDocs.ScoreDocs;
            var totalHits = topDocs.TotalHits;

            var results = scoreDocs
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToDictionary(d => int.Parse(searcher.Doc(d.Doc).Get(MangabakaFields.Id)), d => d.Score);

            var seriesData = await ctx.Series
                .Where(s => results.Keys.Contains(s.Id))
                .Where(s => s.MergedWith == null)
                .ToListAsync(cancellationToken);

            var monitoredSeriesById = (await unitOfWork.MonitoredSeriesRepository
                    .GetByMangaBakaIds(seriesData.Select(s => s.Id.ToString()).ToList(), cancellationToken))
                .ToDictionary(s => s.MangaBakaId, s => s.Id);

            var sortedResults = seriesData
                .OrderByDescending(s => results[s.Id])
                .Select(s => ConvertToSeries(s, monitoredSeriesById))
                .ToList();

            return new PagedList<MetadataSearchResult>(sortedResults, totalHits, paginationParams.PageNumber, paginationParams.PageSize);
        }
        finally
        {
            searcherManager.Release(searcher);
        }
    }

    public async Task<Series?> GetSeries(string externalId, CancellationToken ct)
    {
        if (!int.TryParse(externalId, out var seriesId))
            return null;


        var series = await ctx.Series.FirstOrDefaultAsync(s => s.Id == seriesId, ct);

        var monitoredSeriesById = series != null ? (await unitOfWork.MonitoredSeriesRepository
                .GetByMangaBakaIds([series.Id.ToString()], ct))
            .ToDictionary(s => s.MangaBakaId, s => s.Id) : [];

        return series == null ? null : ConvertToSeries(series, monitoredSeriesById);
    }

    public async Task<List<Cover>> GetCovers(string externalId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(externalId, out var seriesId))
            return [];

        var series = await ctx.Series.FirstOrDefaultAsync(s => s.Id == seriesId, cancellationToken);
        if (series == null) return [];

        List<MangabakaCover> covers = [];

        var pagination = new Pagination
        {
            Next = $"/v1/series/{externalId}/images?page=1&limit=50"
        };

        while (!string.IsNullOrEmpty(pagination?.Next))
        {
            var response = await HttpClient.GetCachedAsync<PaginatedResponse<MangabakaCover>>(pagination.Next,
                cache, cancellationToken: cancellationToken);

            if (response.IsErr)
                throw new MnemaException($"Failed to load covers: {response.Error?.Message}", response.Error);

            var paginatedResponse = response.Unwrap();

            if (paginatedResponse.Status != 200)
                throw new MnemaException($"Failed to load covers: {paginatedResponse.Status} - {paginatedResponse.Reason}");

            pagination = paginatedResponse.Pagination;

            if (paginatedResponse.Data != null)
                covers.AddRange(paginatedResponse.Data.Where(cover => cover.Type == "volume"));

            if (!string.IsNullOrEmpty(pagination?.Next))
                await Task.Delay(100, cancellationToken);
        }

        var languages = series.Titles?
            .Where(t => t.Traits.Contains("native"))
            .Select(t => t.Language)
            .Distinct().ToList() ?? [];

        return covers.GroupBy(c => c.Type + c.Index).Select(g =>
        {
            var preferredCover = g
                .OrderByDescending(cover => cover.Language == "en")
                .ThenByDescending(cover => languages.Contains(cover.Language))
                .FirstOrDefault();

            if (preferredCover == null)
                return null;

            return new Cover
            {
                Extension = preferredCover.Image.RawImage.Format,
                Url = preferredCover.Image.RawImage.Url,
                Volume = preferredCover.Index,
            };
        }).WhereNotNull().ToList();
    }

    private static MetadataSearchResult ConvertToSeries(MangabakaSeries series, Dictionary<string, Guid> monitoredSeriesIds)
    {
        var publishers = series.Publishers?
            .Where(p => p.Type == MangabakaPublisher.Original || p.Type == MangabakaPublisher.English)
            .Select(p => Person.Create(p.Name, PersonRole.Publisher)) ?? [];
        var writers = series.Authors?
            .Select(p => Person.Create(p, PersonRole.Writer)) ?? [];
        var artists = series.Artists?
            .Select(p => Person.Create(p, PersonRole.Colorist)) ?? [];

        return new MetadataSearchResult
        {
            Id = series.Id.ToString(),
            MonitoredSeriesId = monitoredSeriesIds.GetValueOrDefault(series.Id.ToString()),
            Title = series.Titles.FindBestTitle(),
            LocalizedSeries = series.Titles.FindBestNativeTitle(),
            Summary = series.Description ?? string.Empty,
            Status = FromMangabakaPublicationStatus(series.Status),
            RefUrl = $"https://mangabaka.org/{series.Id}",
            Tags = series.Genres?
                .Select(g => new Tag(g, true))
                .ToList() ?? [], // Mangabaka tags are pure nonsense because they have MU
            People = publishers.Concat(writers).Concat(artists).ToList(),
            Links = series.CollectLinks(),
            CoverUrl = series.CoverX350X3,
            Year = series.StartDate?.Year,
            HighestVolumeNumber = series.Status.HasFinalCount() ? series.FinalVolume.AsFloat() : null,
            HighestChapterNumber = series.Status.HasFinalCount() ? series.FinalChapter.AsFloat() : null,
            Chapters = []
        };
    }

    private static PublicationStatus FromMangabakaPublicationStatus(MangabakaPublicationStatus publicationStatus)
    {
        return publicationStatus switch
        {
            MangabakaPublicationStatus.Completed => PublicationStatus.Completed,
            MangabakaPublicationStatus.Releasing => PublicationStatus.Ongoing,
            MangabakaPublicationStatus.Cancelled => PublicationStatus.Cancelled,
            MangabakaPublicationStatus.Hiatus => PublicationStatus.Paused,
            MangabakaPublicationStatus.Upcoming => PublicationStatus.Ongoing, // Close enough
            _ => throw new ArgumentOutOfRangeException(nameof(publicationStatus), publicationStatus, null)
        };
    }

    [GeneratedRegex(@"[^a-z0-9\s]")]
    private static partial Regex TermNormalisationRegex();
}
