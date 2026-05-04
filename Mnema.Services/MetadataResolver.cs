using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Services;

public class MetadataResolver(
    ISettingsService settingsService,
    IParserService parserService,
    [FromKeyedServices(key: MetadataProvider.Hardcover)] IMetadataProviderService hardcoverMetadataProvider,
    [FromKeyedServices(key: MetadataProvider.Mangabaka)] IMetadataProviderService mangabakaMetadataProvider,
    IServiceProvider serviceProvider
    ): IMetadataResolver
{
    public async Task<Series?> ResolveSeriesAsync(Provider provider, MetadataBag metadata,
        CancellationToken cancellationToken = default)
    {
        var hardCoverId = metadata.GetKey(RequestConstants.HardcoverSeriesIdKey);
        var mangaBakaId = metadata.GetKey(RequestConstants.MangaBakaKey);
        var externalId = metadata.GetKey(RequestConstants.ExternalIdKey);

        Dictionary<MetadataProvider, Series?> series = [];

        if (!string.IsNullOrEmpty(hardCoverId))
        {
            series[MetadataProvider.Hardcover] = await hardcoverMetadataProvider.GetSeries(hardCoverId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(mangaBakaId))
        {
            series[MetadataProvider.Mangabaka] = await mangabakaMetadataProvider.GetSeries(mangaBakaId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(externalId))
        {
            var repo = serviceProvider.GetKeyedService<IRepository>(provider);
            if (repo != null)
            {
                series[MetadataProvider.Upsteam] = await repo.SeriesInfo(new DownloadRequestDto
                {
                    Provider = provider,
                    Id = externalId,
                    BaseDir = string.Empty,
                    TempTitle = string.Empty,
                    Metadata = metadata
                }, cancellationToken);
            }
        }

        var settings = await settingsService.GetSettingsAsync();

        var mergedSeries = MergeSeries(series, settings, metadata);
        if (mergedSeries == null || !metadata.GetKey(MetadataResolverOptions.EnrichWithCovers))
            return mergedSeries;

        if (!string.IsNullOrEmpty(mangaBakaId))
        {
            var covers = await mangabakaMetadataProvider.GetCovers(mangaBakaId, cancellationToken);

            InsertCovers(mergedSeries, covers);
        }

        if (!string.IsNullOrEmpty(hardCoverId))
        {
            var covers = await hardcoverMetadataProvider.GetCovers(hardCoverId, cancellationToken);

            InsertCovers(mergedSeries, covers);
        }

        return mergedSeries;
    }

    private static Series? MergeSeries(Dictionary<MetadataProvider, Series?> series, ServerSettingsDto settings, MetadataBag metadata)
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

        var mergedIntoUpstream = metadata.GetKey(MetadataResolverOptions.MergeIntoUpstream);

        if (mergedIntoUpstream && series.TryGetValue(MetadataProvider.Upsteam, out var upstreamSeries))
        {
            mergedSeries = upstreamSeries;
        }

        var sorted = settings.MetadataProviderSettings
            .Where(kv => kv.Value.Enabled)
            .Where(kv => !(mergedIntoUpstream && kv.Key == MetadataProvider.Upsteam))
            .OrderBy(kv => kv.Value.Priority);

        foreach (var (metadataProvider, setting) in sorted)
        {
            if (!series.TryGetValue(metadataProvider, out var seriesEntity) || seriesEntity == null)
            {
                continue;
            }

            Merge(metadataProvider, mergedSeries, seriesEntity, setting.SeriesSettings);
        }

        return mergedSeries;
    }

    private static void Merge(MetadataProvider provider, Series into, Series from, SeriesMetadataSettingsDto settings)
    {
        if (string.IsNullOrEmpty(into.Id) && !string.IsNullOrEmpty(from.Id))
        {
            into.Id = from.Id;
        }

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

        if (string.IsNullOrEmpty(into.RefUrl))
        {
            into.RefUrl = from.RefUrl;
        }
        else if (!string.IsNullOrEmpty(from.RefUrl) && !into.Links.Contains(from.RefUrl))
        {
            into.Links.Add(from.RefUrl);
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
                .DistinctBy(t => t.Value.ToNormalized())
                .ToList();
        }

        if (settings.People)
        {
            into.People = into.People.Concat(from.People)
                .DistinctBy(p => p.Name.ToNormalized())
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
                    continue;
                }

                match ??= new Chapter
                {
                    Id = string.Empty,
                    Title = string.Empty,
                    VolumeMarker = string.Empty,
                    ChapterMarker = string.Empty,
                    Tags = [],
                    People = [],
                    TranslationGroups = []
                };

                MergeChapter(match, fromChapter, settings.ChapterSettings);
                into.Chapters.Add(match);
            }
        }
    }

    private static void MergeChapter(Chapter into, Chapter from, ChapterMetadataSettingsDto settings)
    {

        if (string.IsNullOrEmpty(into.Id))
        {
            into.Id = from.Id;
        }

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

            into.TranslationGroups = into.TranslationGroups
                .Concat(from.TranslationGroups)
                .Distinct()
                .ToList();
        }

        if (settings.Tags)
        {
            into.Tags = into.Tags.Concat(from.Tags)
                .DistinctBy(t => t.Value.ToNormalized())
                .ToList();
        }

        if (string.IsNullOrEmpty(into.VolumeMarker))
        {
            into.VolumeMarker = from.VolumeMarker;
        }

        if (string.IsNullOrEmpty(into.ChapterMarker))
        {
            into.ChapterMarker = from.ChapterMarker;
        }

        into.SortOrder ??= from.SortOrder;

        if (string.IsNullOrEmpty(into.RefUrl))
        {
            into.RefUrl = from.RefUrl;
        }

    }

    private static void InsertCovers(Series series, List<Cover> covers)
    {
        foreach (var cover in covers)
        {
            var chapters = series.Chapters.Where(c =>
            {
                if (string.IsNullOrEmpty(cover.Chapter) && MatchingIfNotNull(c.VolumeMarker, cover.Volume))
                    return true;

                return MatchingIfNotNull(c.VolumeMarker, cover.Volume) && MatchingIfNotNull(cover.Chapter, c.ChapterMarker);
            });

            foreach (var chapter in chapters)
            {
                chapter.CoverUrl = cover.Url;
                chapter.CoverFileFormat = "." + cover.Extension;
            }

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

        var match = series.Chapters.FirstOrDefault(c =>
        {
            if (string.IsNullOrEmpty(chapter) && MatchingIfNotNull(c.VolumeMarker, volume))
                return true;

            return MatchingIfNotNull(c.VolumeMarker, volume) && MatchingIfNotNull(chapter, c.ChapterMarker);
        });

        if (match != null)
            return match;

        var matchingVolumes = series.Chapters
            .Where(c => MatchingIfNotNull(c.VolumeMarker, volume))
            .ToList();

        return matchingVolumes.Count == 1 ? matchingVolumes[0] : null;
    }

    private static bool MatchingIfNotNull(string? first, string? second)
        => !string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(second) && first == second;
}
