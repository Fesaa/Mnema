using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.External;
using Mnema.Models.Publication;

namespace Mnema.Metadata.Mangabaka;

internal class MangabakaMetadataService(
    ILogger<MangabakaMetadataService> logger,
    MangabakaDbContext ctx
): IMetadataProviderService
{
    public async Task<List<Series>> Search(MetadataSearchDto search, CancellationToken cancellationToken)
    {
        var matchedSeries = await ctx.Series
            .Where(s => s.MergedWith == null)
            .Where(s => EF.Functions.Like(s.Title, $"%{search.Query}%")
            || EF.Functions.Like(s.SecondaryTitlesEn, $"%{search.Query}%"))
            .ToListAsync(cancellationToken);

        return matchedSeries.Select(ConvertToSeries).ToList();
    }

    public async Task<Series?> GetSeries(string externalId, CancellationToken ct)
    {
        if (!int.TryParse(externalId, out var seriesId))
            return null;


        var series = await ctx.Series.FirstOrDefaultAsync(s => s.Id == seriesId, ct);
        return series == null ? null : ConvertToSeries(series);
    }

    private static Series ConvertToSeries(MangabakaSeries series)
    {
        var publishers = series.Publishers?
            .Select(p => Person.Create(p.Name, PersonRole.Publisher)) ?? [];
        var writers = series.Authors?
            .Select(p => Person.Create(p, PersonRole.Writer)) ?? [];
        var artists = series.Artists?
            .Select(p => Person.Create(p, PersonRole.Colorist)) ?? [];

        return new Series
        {
            Id = series.Id.ToString(),
            Title = series.Title,
            LocalizedSeries = series.NativeTitle,
            Summary = series.Description ?? string.Empty,
            Status = FromMangabakaPublicationStatus(series.Status),
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
            MangabakaPublicationStatus.Upcoming => PublicationStatus.Ongoing, // Close enought
            _ => throw new ArgumentOutOfRangeException(nameof(publicationStatus), publicationStatus, null)
        };
    }
}
