using System;
using System.Collections.Generic;
using System.Linq;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.User;
using Mnema.Models.External;
using Mnema.Models.Publication;

namespace Mnema.Providers.Services;

internal class MetadataService : IMetadataService
{
    public ComicInfo? CreateComicInfo(UserPreferences preferences, DownloadRequestDto request, string title, Series? series,
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

        ci.Web = string.Join(',', series.Links.Concat([series.RefUrl]).Distinct());

        var allTags = series.Tags.Concat(chapter?.Tags ?? []).ToList();

        var (genres, tags) = ProcessTags(preferences, allTags, request);
        ci.Genre = string.Join(',', genres);
        ci.Tags = string.Join(',', tags);

        var ar = GetAgeRating(preferences, allTags);
        ar = series.AgeRating > ar ? series.AgeRating : ar;
        if (ar != null) ci.AgeRating = ar.Value;

        var (count, finished) = GetCount(series);

        if (count == null) return ci;

        ci.Count = count.Value;
        ci.Finished = finished;

        return ci;
    }

    public (List<string>, List<string>) ProcessTags(
        UserPreferences preferences, IList<Tag> inputTags, DownloadRequestDto request)
    {
        var mapToGenre = preferences.ConvertToGenreList.Select(g => g.ToNormalized()).ToList();
        var blackListed = preferences.BlackListedTags.Select(g => g.ToNormalized()).ToList();
        var whiteListed = preferences.WhiteListedTags.Select(g => g.ToNormalized()).ToList();

        var finalInputTags = MapTags(inputTags, preferences.TagMappings);

        var filteredGenres = finalInputTags
            .Where(TagAllowedAsGenre)
            .Select(t => t.Value)
            .Distinct()
            .ToList();
        var filteredTags = finalInputTags
            .Where(TagAllowedAsTag)
            .Select(t => t.Value)
            .Distinct()
            .ToList();

        return (filteredGenres, filteredTags);

        bool TagAllowedAsGenre(Tag tag)
        {
            var tagValue = tag.Value.ToNormalized();
            var tagId = tag.Id.ToNormalized();

            var isBlackListed = blackListed.Contains(tagValue) || blackListed.Contains(tagId);
            if (isBlackListed) return false;

            return tag.IsMarkedAsGenre || mapToGenre.Contains(tagValue) || mapToGenre.Contains(tagId);
        }

        bool TagAllowedAsTag(Tag tag)
        {
            var tagValue = tag.Value.ToNormalized();
            var tagId = tag.Id.ToNormalized();

            var isBlackListed = blackListed.Contains(tagValue) || blackListed.Contains(tagId);
            if (isBlackListed) return false;

            if (TagAllowedAsGenre(tag)) return false;

            if (request.GetBool(RequestConstants.IncludeNotMatchedTagsKey) && whiteListed.Count == 0) return true;

            return whiteListed.Contains(tagValue) || whiteListed.Contains(tagId);
        }
    }

    public AgeRating? GetAgeRating(UserPreferences preferences, IList<Tag> inputTags)
    {
        var ageRatingMappings = preferences.AgeRatingMappings.Select(m => m with
        {
            Tag = m.Tag.ToNormalized()
        }).ToList();

        var finalInputTags = MapTags(inputTags, preferences.TagMappings);

        var ageRatings = finalInputTags
            .Select(GetAgeRatingForTag)
            .WhereNotNull()
            .ToList();

        return ageRatings.Count == 0 ? null : ageRatings.Max();

        AgeRating? GetAgeRatingForTag(Tag tag)
        {
            var tagValue = tag.Value.ToNormalized();
            var tagId = tag.Id.ToNormalized();

            var tagAgeRating = ageRatingMappings
                .Where(mapping => mapping.Tag == tagValue || mapping.Tag == tagId)
                .Aggregate(AgeRating.Unknown,
                    (current, mapping) => current > mapping.AgeRating ? current : mapping.AgeRating);

            return tagAgeRating > AgeRating.Unknown ? tagAgeRating : null;
        }
    }

    public List<Tag> MapTags(IList<Tag> tags, IList<TagMappingDto> mappings)
    {
        mappings = mappings.Select(m => m with
        {
            OriginTag = m.OriginTag.ToNormalized()
        }).ToList();

        return tags.Select(tag =>
        {
            var tagValue = tag.Value.ToNormalized();
            var tagId = tag.Id.ToNormalized();

            return new Tag
            {
                Id = mappings.FirstOrDefault(m => m.OriginTag == tagId)?.DestinationTag ?? tag.Id,
                Value = mappings.FirstOrDefault(m => m.OriginTag == tagValue)?.DestinationTag ?? tag.Value,
                IsMarkedAsGenre = tag.IsMarkedAsGenre
            };
        }).ToList();
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
            return ((int?)series.HighestVolumeNumber, series.HighestVolumeNumber.SafeEquals(highestVolume));

        if (series.HighestChapterNumber != null)
            return ((int?)series.HighestChapterNumber, series.HighestChapterNumber.SafeEquals(highestChapter));

        if (highestVolume != null) return ((int?)highestVolume, true);

        if (highestChapter != null) return ((int?)highestChapter, true);

        return (null, false);
    }
}
