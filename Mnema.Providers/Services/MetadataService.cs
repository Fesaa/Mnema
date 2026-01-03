using System.Collections.Generic;
using System.Linq;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.User;
using Mnema.Models.Publication;

namespace Mnema.Providers.Services;

internal class MetadataService: IMetadataService
{
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
            Tag = m.Tag.ToNormalized(),
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
            OriginTag = m.OriginTag.ToNormalized(),
        }).ToList();
        
        return tags.Select(tag =>
        {
            var tagValue = tag.Value.ToNormalized();
            var tagId = tag.Id.ToNormalized();

            return new Tag
            {
                Id = mappings.FirstOrDefault(m => m.OriginTag == tagId)?.DestinationTag ?? tag.Id,
                Value = mappings.FirstOrDefault(m => m.OriginTag == tagValue)?.DestinationTag ?? tag.Value,
                IsMarkedAsGenre = tag.IsMarkedAsGenre,
            };
        }).ToList();
    }
}