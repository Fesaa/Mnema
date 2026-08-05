using System;
using System.Collections.Generic;
using System.Linq;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Enums;
using Mnema.Models.External;
using Mnema.Models.Publication;

namespace Mnema.Providers.Services;

internal class StringLinkInfoImplementation(string url) : ILinkInfo
{
    public string Url => url;
    public string Language => string.Empty;
}

internal class MetadataService : IMetadataService
{
    public ComicInfo? CreateComicInfo(Preferences preferences, DownloadRequestDto request, string title, Series? series,
        Chapter? chapter, string? note = null)
    {
        if (series == null) return null;

        var ci = new ComicInfo
        {
            Series = title,
            LocalizedSeries = series.LocalizedSeries ?? string.Empty,
            Summary = chapter != null ? chapter.Summary.OrNonEmpty(series.Summary) : series.Summary,
            Title = chapter?.Title ?? string.Empty,
        };

        if (note != null)
            ci.Notes = note;

        if (chapter != null)
        {
            if (chapter.VolumeNumber() != null) ci.Volume = chapter.VolumeMarker;

            if (chapter.IsOneShot)
                ci.Format = "Special";
            else
                ci.Number = chapter.ChapterMarker;

            if (!string.IsNullOrEmpty(chapter.Isbn))
                ci.Isbn = chapter.Isbn;
        }

        foreach (var role in Enum.GetValues<PersonRole>())
        {
            var value = string.Join(',', series.People
                .Concat(chapter?.People ?? [])
                .Where(p => p.Roles.Contains(role))
                .DistinctBy(p => p.Name)
                .Select(p => p.Name));

            ci.SetForRole(value, role);
        }

        var allLinks = new List<string>(series.Links);
        if (!string.IsNullOrEmpty(series.RefUrl))
        {
            allLinks.Add(series.RefUrl);
        }

        if (!string.IsNullOrEmpty(chapter?.RefUrl))
        {
            allLinks.Add(chapter.RefUrl);
        }

        ci.Web = CollectLinks(preferences, allLinks);

        var allTags = series.Tags.Concat(chapter?.Tags ?? []).ToList();

        var (genres, tags) = GenerateGenreAndTagLists(preferences, allTags);
        ci.Genre = string.Join(',', genres);
        ci.Tags = string.Join(',', tags);

        var ar = GetAgeRating(preferences, allTags);
        ar = series.AgeRating > ar || ar == null ? series.AgeRating : ar;
        if (ar != null) ci.AgeRating = ar.Value;

        var (count, finished) = GetCount(series);

        if (count == null) return ci;

        ci.Count = count.Value;
        ci.Finished = finished;

        return ci;
    }

    internal static string CollectLinks(Preferences preferences, List<string> links)
    {
        var filters = preferences.LinkFilters.Where(f => f.Type == LinkFilterType.Hostname).ToList();

        return string.Join(',', links
            .Select(l => new StringLinkInfoImplementation(l))
            .Where(l => LinkFilter.IsAllowed(l, filters))
        );
    }

    #region Genre and Tags Mappings (Mostly Kavita copied code)

    internal static (List<string> Genres, List<string> Tags) GenerateGenreAndTagLists(Preferences preferences, List<Tag> allTags, bool applyBlackAndWhiteLists = true)
    {
        var genres = allTags.Where(t => t.IsMarkedAsGenre).Select(t => t.Value).Distinct().ToList();
        var tags = allTags.Where(t => !t.IsMarkedAsGenre).Select(t => t.Value).Distinct().ToList();

        var processedGenres = new List<string>();
        var processedTags = new List<string>();

        var mappings = ApplyFieldMappings(tags, MetadataFieldType.Tag, preferences.MetadataFieldMappings);
        if (mappings.TryGetValue(MetadataFieldType.Tag, out var tagsToTags))
        {
            processedTags.AddRange(tagsToTags);
        }
        if (mappings.TryGetValue(MetadataFieldType.Genre, out var tagsToGenres))
        {
            processedGenres.AddRange(tagsToGenres);
        }

        mappings = ApplyFieldMappings(genres, MetadataFieldType.Genre, preferences.MetadataFieldMappings);
        if (mappings.TryGetValue(MetadataFieldType.Tag, out var genresToTags))
        {
            processedTags.AddRange(genresToTags);
        }
        if (mappings.TryGetValue(MetadataFieldType.Genre, out var genresToGenres))
        {
            processedGenres.AddRange(genresToGenres);
        }

        if (applyBlackAndWhiteLists)
        {
            processedTags = ApplyBlackWhiteList(preferences, MetadataFieldType.Tag, processedTags);
            processedGenres = ApplyBlackWhiteList(preferences, MetadataFieldType.Genre, processedGenres);
        }

        return (processedGenres, processedTags);
    }

    private static Dictionary<MetadataFieldType, List<string>> ApplyFieldMappings(IEnumerable<string> values, MetadataFieldType sourceType, IList<MetadataFieldMappingDto> mappings)
    {
        var result = new Dictionary<MetadataFieldType, List<string>>();

        foreach (var field in Enum.GetValues<MetadataFieldType>())
        {
            result[field] = [];
        }

        foreach (var value in values)
        {
            var matchingMappings = mappings.Where(m =>
                m.SourceType == sourceType &&
                m.SourceValue.ToNormalized().Equals(value.ToNormalized()));

            var keepOriginal = true;

            foreach (var mapping in matchingMappings.Where(mapping => !string.IsNullOrWhiteSpace(mapping.DestinationValue)))
            {
                result[mapping.DestinationType].Add(mapping.DestinationValue);

                // Only keep the original tags if none of the matches want to remove it
                keepOriginal = keepOriginal && !mapping.ExcludeFromSource;
            }

            if (keepOriginal)
            {
                result[sourceType].Add(value);
            }
        }

        // Ensure distinct
        foreach (var key in result.Keys)
        {
            result[key] = result[key].Distinct().ToList();
        }

        return result;
    }

    private static List<string> ApplyBlackWhiteList(Preferences preferences, MetadataFieldType fieldType, List<string> processedStrings)
    {
        var whiteList = preferences.WhiteListedTags.Select(t => t.ToNormalized()).ToList();
        var blackList = preferences.BlackListedTags.Select(t => t.ToNormalized()).ToList();

        return fieldType switch
        {
            MetadataFieldType.Genre => processedStrings.Distinct()
                .Where(g => blackList.Count == 0 || !blackList.Contains(g.ToNormalized()))
                .ToList(),
            MetadataFieldType.Tag => processedStrings.Distinct()
                .Where(g => blackList.Count == 0 || !blackList.Contains(g.ToNormalized()))
                .Where(g => whiteList.Count == 0 || whiteList.Contains(g.ToNormalized()))
                .ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, null),
        };
    }

    #endregion

    internal static AgeRating? GetAgeRating(Preferences preferences, List<Tag> inputTags)
    {
        var ageRatingMappings = preferences.AgeRatingMappings.Select(m => m with
        {
            Tag = m.Tag.ToNormalized()
        }).ToList();

        var (genres, tags) = GenerateGenreAndTagLists(preferences, inputTags, false);
        var allTags = genres.Concat(tags).ToList();

        var ageRatings = allTags
            .Select(GetAgeRatingForTag)
            .WhereNotNull()
            .ToList();

        return ageRatings.Count == 0 ? null : ageRatings.Max();

        AgeRating? GetAgeRatingForTag(string tag)
        {
            var tagValue = tag.ToNormalized();

            var tagAgeRating = ageRatingMappings
                .Where(mapping => mapping.Tag == tagValue)
                .Aggregate(AgeRating.Unknown,
                    (current, mapping) => current > mapping.AgeRating ? current : mapping.AgeRating);

            return tagAgeRating > AgeRating.Unknown ? tagAgeRating : null;
        }
    }

    private static (int?, bool) GetCount(Series? series)
    {
        if (series == null) return (null, false);

        if (series.Status != PublicationStatus.Completed) return (null, false);

        if (series.TranslationStatus != null && series.TranslationStatus != PublicationStatus.Completed)
            return (null, false);

        var chapterNumbers = series.Chapters.Select(c => c.ChapterNumber()).WhereNotNull().ToList();
        var volumeNumbers = series.Chapters.Select(c => c.VolumeNumber()).WhereNotNull().ToList();

        var highestChapter = chapterNumbers.Count == 0 ? null : chapterNumbers.Max();
        var highestVolume = volumeNumbers.Count == 0 ? null : volumeNumbers.Max();

        if (series.HighestVolumeNumber != null)
        {
            if (series.HighestChapterNumber == null)
                return ((int?)series.HighestVolumeNumber, series.HighestVolumeNumber.SafeEquals(highestVolume));

            var everythingDownloaded = series.HighestVolumeNumber.SafeEquals(highestVolume) && series.HighestChapterNumber.SafeEquals(highestChapter);
            return ((int?)series.HighestVolumeNumber, everythingDownloaded);
        }

        if (series.HighestChapterNumber != null)
            return ((int?)series.HighestChapterNumber, series.HighestChapterNumber.SafeEquals(highestChapter));

        if (highestVolume != null) return ((int?)highestVolume, true);

        if (highestChapter != null) return ((int?)highestChapter, true);

        return (null, false);
    }
}
