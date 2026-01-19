using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Services;

public class MetadataResolver(
    IParserService parserService,
    [FromKeyedServices(key: MetadataProvider.Hardcover)] IMetadataProviderService hardcoverMetadataProvider,
    [FromKeyedServices(key: MetadataProvider.Mangabaka)] IMetadataProviderService mangabakaMetadataProvider
    ): IMetadataResolver
{
    public async Task<Series?> ResolveSeriesAsync(MetadataBag metadata, CancellationToken cancellationToken = default)
    {
        var hardCoverId = metadata.GetString(RequestConstants.HardcoverSeriesIdKey);
        var mangaBakaId = metadata.GetString(RequestConstants.MangaBakaKey);

        Series? series;
        if (!string.IsNullOrEmpty(mangaBakaId))
        {
            series = await mangabakaMetadataProvider.GetSeries(mangaBakaId, cancellationToken);
            if (series != null)
                return series;
        }

        if (!string.IsNullOrEmpty(hardCoverId))
        {
            series = await hardcoverMetadataProvider.GetSeries(hardCoverId, cancellationToken);
            if (series != null)
                return series;
        }

        return null;
    }

    public ChapterResolutionResult ResolveChapter(string fileName, Series? series, ContentFormat contentFormat)
    {
        var volume = parserService.ParseVolume(fileName, contentFormat);
        var chapter = parserService.ParseChapter(fileName, contentFormat);

        volume = parserService.IsLooseLeafVolume(volume) ? null : volume;
        chapter = parserService.IsDefaultChapter(chapter) ? string.Empty : chapter;

        var chapterEntity = FindChapter(series, volume, chapter);

        return new ChapterResolutionResult(volume, chapter, chapterEntity);
    }

    private static Chapter? FindChapter(Series? series, string? volume, string? chapter)
    {
        if (series == null || (string.IsNullOrEmpty(chapter) && string.IsNullOrEmpty(volume)))
            return null;

        return series.Chapters.FirstOrDefault(c =>
        {
            if (string.IsNullOrEmpty(chapter) && MatchingIfNotNull(c.VolumeMarker, volume))
                return true;

            return MatchingIfNotNull(c.VolumeMarker, volume) && MatchingIfNotNull(chapter, c.ChapterMarker);
        });

        static bool MatchingIfNotNull(string? first, string? second)
            => !string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(second) && first == second;
    }
}
