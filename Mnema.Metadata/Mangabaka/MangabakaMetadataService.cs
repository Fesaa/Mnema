using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.External;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Metadata.Mangabaka;

internal class MangabakaMetadataService(
    ILogger<MangabakaMetadataService> logger,
    IUnitOfWork unitOfWork,
    MangabakaDbContext ctx,
    [FromKeyedServices(key: MetadataProvider.Mangabaka)] SearcherManager searcherManager
): IMetadataProviderService
{
    public async Task<PagedList<MetadataSearchResult>> Search(MetadataSearchDto search, PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        var searcher = searcherManager.Acquire();
        try
        {
            var analyzer = new StandardAnalyzer(MangabakaScheduler.Version);
            var fields = new[] { nameof(MangabakaSeries.Title), nameof(MangabakaSeries.NativeTitle) };

            var parser = new MultiFieldQueryParser(MangabakaScheduler.Version, fields, analyzer)
            {
                AllowLeadingWildcard = true,
                DefaultOperator = Operator.AND,
            };

            var query = parser.Parse($"{search.Query}*");

            var hitsToFetch = (paginationParams.PageNumber + 1) * paginationParams.PageSize;
            var topDocs = searcher.Search(query, hitsToFetch);

            var scoreDocs = topDocs.ScoreDocs;
            var totalHits = topDocs.TotalHits;

            var results = scoreDocs
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToDictionary(d => int.Parse(searcher.Doc(d.Doc).Get(nameof(MangabakaSeries.Id))), d => d.Score);

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

    private static MetadataSearchResult ConvertToSeries(MangabakaSeries series, Dictionary<string, Guid> monitoredSeriesIds)
    {
        var publishers = series.Publishers?
            .Select(p => Person.Create(p.Name, PersonRole.Publisher)) ?? [];
        var writers = series.Authors?
            .Select(p => Person.Create(p, PersonRole.Writer)) ?? [];
        var artists = series.Artists?
            .Select(p => Person.Create(p, PersonRole.Colorist)) ?? [];

        return new MetadataSearchResult
        {
            Id = series.Id.ToString(),
            MonitoredSeriesId = monitoredSeriesIds.GetValueOrDefault(series.Id.ToString()),
            Title = series.Title,
            LocalizedSeries = series.NativeTitle,
            Summary = series.Description ?? string.Empty,
            Status = FromMangabakaPublicationStatus(series.Status),
            RefUrl = $"https://mangabaka.org/{series.Id}",
            Tags = series.Genres?
                .Select(g => new Tag(g, true))
                .ToList() ?? [], // Mangabaka tags are pure nonsense because they have MU
            People = publishers.Concat(writers).Concat(artists).ToList(),
            Links = series.Links ?? [],
            CoverUrl = series.CoverX350X3,
            Year = series.Year,
            HighestVolumeNumber = series.FinalVolume.AsFloat(),
            HighestChapterNumber = series.FinalChapter.AsFloat(),
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
}
