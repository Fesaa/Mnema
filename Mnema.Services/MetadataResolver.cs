using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Services;

public class MetadataResolver(
    ISettingsService settingsService,
    IParserService parserService,
    [FromKeyedServices(key: MetadataProvider.Hardcover)] IMetadataProviderService hardcoverMetadataProvider,
    [FromKeyedServices(key: MetadataProvider.Mangabaka)] IMetadataProviderService mangabakaMetadataProvider
    ): IMetadataResolver
{
    public async Task<Series?> ResolveSeriesAsync(MetadataBag metadata, CancellationToken cancellationToken = default)
    {
        var hardCoverId = metadata.GetString(RequestConstants.HardcoverSeriesIdKey);
        var mangaBakaId = metadata.GetString(RequestConstants.MangaBakaKey);

        Dictionary<MetadataProvider, Series?> series = [];

        if (!string.IsNullOrEmpty(hardCoverId))
        {
            series[MetadataProvider.Hardcover] = await hardcoverMetadataProvider.GetSeries(hardCoverId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(mangaBakaId))
        {
            series[MetadataProvider.Mangabaka] = await mangabakaMetadataProvider.GetSeries(mangaBakaId, cancellationToken);
        }

        var settings = await settingsService.GetSettingsAsync();

        return MergeSeries(series, settings);
    }

    private static Series? MergeSeries(Dictionary<MetadataProvider, Series?> series, ServerSettingsDto settings)
    {
        if (series.All(kv => kv.Value == null))
            return null;

        var mergedSeries = new Series
        {
            Id = string.Empty,
            Title = string.Empty,
            Summary = string.Empty,
            Status = PublicationStatus.Unknown,
            Tags = [],
            People = [],
            Links = [],
            Chapters = []
        };

        var sorted = settings.MetadataProviderSettings
            .Where(kv => kv.Value.Enabled)
            .OrderBy(kv => kv.Value.Priority);

        foreach (var (metadataProvider, setting) in sorted)
        {
            if (!series.TryGetValue(metadataProvider, out var seriesEntity) || seriesEntity == null)
            {
                continue;
            }

            Merge(mergedSeries, seriesEntity, setting.SeriesSettings);
        }

        return mergedSeries;
    }

    private static void Merge(Series into, Series from, SeriesMetadataSettingsDto settings)
    {
        if (settings.Title && string.IsNullOrEmpty(into.Title))
        {
            into.Title = from.Title;
        }

        if (settings.Summary && string.IsNullOrEmpty(into.Summary))
        {
            into.Summary = from.Summary;
        }

        if (settings.LocalizedSeries && string.IsNullOrEmpty(into.LocalizedSeries))
        {
            into.LocalizedSeries = from.LocalizedSeries;
        }

        if (settings.CoverUrl && string.IsNullOrEmpty(into.CoverUrl))
        {
            into.CoverUrl = from.CoverUrl;
        }

        if (settings.PublicationStatus && into.Status == PublicationStatus.Unknown)
        {
            into.Status = from.Status;
        }

        if (settings.Year && into.Year == null)
        {
            into.Year = from.Year;
        }

        if (settings.AgeRating && into.AgeRating == null || into.AgeRating == AgeRating.Unknown)
        {
            into.AgeRating = from.AgeRating;
        }

        if (settings.Tags)
        {
            into.Tags = into.Tags
                .Concat(from.Tags)
                .DistinctBy(t => t.Value)
                .ToList();
        }

        if (settings.People)
        {
            into.People = into.People.Concat(from.People)
                .DistinctBy(p => p.Name)
                .ToList();
        }

        if (settings.Links)
        {
            into.Links = into.Links.Concat(from.Links)
                .Distinct()
                .ToList();
        }

        if (settings.Chapters)
        {
            foreach (var fromChapter in from.Chapters)
            {
                var match = into.Chapters.FirstOrDefault(c
                    => c.VolumeMarker == fromChapter.VolumeMarker
                    && c.ChapterMarker == fromChapter.ChapterMarker);
                if (match != null)
                {
                    MergeChapter(match, fromChapter, settings.ChapterSettings);
                }
                else
                {
                    into.Chapters.Add(fromChapter);
                }
            }
        }
    }

    private static void MergeChapter(Chapter into, Chapter from, ChapterMetadataSettingsDto settings)
    {
        if (settings.Title && string.IsNullOrEmpty(into.Title))
        {
            into.Title = from.Title;
        }

        if (settings.Summary && string.IsNullOrEmpty(into.Summary))
        {
            into.Summary = from.Summary;
        }

        if (settings.Cover && string.IsNullOrEmpty(into.CoverUrl))
        {
            into.CoverUrl = from.CoverUrl;
        }

        if (settings.ReleaseDate && into.ReleaseDate == null)
        {
            into.ReleaseDate = from.ReleaseDate;
        }

        if (settings.People)
        {
            into.People = into.People.Concat(from.People)
                .DistinctBy(p => p.Name)
                .ToList();
        }
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
