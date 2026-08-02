using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;
using Mnema.Models.Enums;
using Mnema.Models.Publication;
using Mnema.Providers.Services;

namespace Mnema.Tests.Providers.Services;

[TestSubject(typeof(MetadataService))]
public class MetadataServiceTest
{
    private static IMetadataService CreateSut()
    {
        return new MetadataService();
    }

    private static DownloadRequestDto Request()
    {
        return new DownloadRequestDto
        {
            Provider = Provider.Nyaa,
            Id = string.Empty,
            BaseDir = string.Empty,
            TempTitle = string.Empty,
            Metadata = new MetadataBag
            {
                [RequestConstants.IncludeNotMatchedTagsKey.Key] = ["true"]
            }
        };
    }

    private static Preferences CreateDefaultPreferences(
        IList<TagMappingDto>? tagMappings = null,
        IList<AgeRatingMappingDto>? ageRatings = null,
        IList<string>? genres = null,
        IList<string>? blacklist = null,
        IList<string>? whitelist = null)
    {
        return new Preferences
        {
            Id = Guid.NewGuid(),
            ImageFormat = ImageFormat.Upstream,
            CoverFallbackMethod = CoverFallbackMethod.None,
            ConvertToGenreList = genres ?? [],
            BlackListedTags = blacklist ?? [],
            WhiteListedTags = whitelist ?? [],
            AgeRatingMappings = ageRatings ?? [],
            TagMappings = tagMappings ?? [],
            MetadataFieldMappings = [],
            PinSubscriptionTitles = false
        };
    }

    private static Tag TagOf(string value)
    {
        return new Tag { Value = value };
    }

    #region GetAgeRating Tests

    [Fact]
    public void GetAgeRating_Returns_Highest_Mapped_AgeRating()
    {
        var preferences = CreateDefaultPreferences(
            ageRatings: new List<AgeRatingMappingDto>
            {
                new() { Tag = "violence", AgeRating = AgeRating.Teen },
                new() { Tag = "nudity", AgeRating = AgeRating.Mature }
            }
        );

        var tags = new List<Tag>
        {
            TagOf("Violence"),
            TagOf("Nudity")
        };

        var rating = MetadataService.GetAgeRating(preferences, tags);

        Assert.Equal(AgeRating.Mature, rating);
    }

    [Fact]
    public void GetAgeRating_Returns_Null_When_No_Tags_Match()
    {
        var preferences = CreateDefaultPreferences(
            ageRatings: new List<AgeRatingMappingDto>
            {
                new() { Tag = "violence", AgeRating = AgeRating.Teen }
            }
        );

        var tags = new List<Tag> { TagOf("Romance") };

        var rating = MetadataService.GetAgeRating(preferences, tags);

        Assert.Null(rating);
    }

    [Fact]
    public void GetAgeRating_Returns_Null_When_No_Input_Tags()
    {
        var preferences = CreateDefaultPreferences(
            ageRatings: new List<AgeRatingMappingDto>
            {
                new() { Tag = "violence", AgeRating = AgeRating.Teen }
            }
        );

        var tags = new List<Tag>();

        var rating = MetadataService.GetAgeRating(preferences, tags);

        Assert.Null(rating);
    }

    [Fact]
    public void GetAgeRating_Returns_Null_When_No_Mappings_Configured()
    {
        var preferences = CreateDefaultPreferences(ageRatings: new List<AgeRatingMappingDto>());

        var tags = new List<Tag> { TagOf("Violence") };

        var rating = MetadataService.GetAgeRating(preferences, tags);

        Assert.Null(rating);
    }

    [Fact]
    public void GetAgeRating_Uses_Normalized_Matching()
    {
        var preferences = CreateDefaultPreferences(
            ageRatings: new List<AgeRatingMappingDto>
            {
                new() { Tag = "VIOLENCE", AgeRating = AgeRating.Mature }
            }
        );

        var tags = new List<Tag> { TagOf("violence") };

        var rating = MetadataService.GetAgeRating(preferences, tags);

        Assert.Equal(AgeRating.Mature, rating);
    }

    [Fact]
    public void GetAgeRating_Returns_Highest_Rating_From_Multiple_Matches()
    {
        var preferences = CreateDefaultPreferences(
            ageRatings: new List<AgeRatingMappingDto>
            {
                new() { Tag = "mild", AgeRating = AgeRating.Teen },
                new() { Tag = "violence", AgeRating = AgeRating.Mature },
                new() { Tag = "graphic", AgeRating = AgeRating.AdultsOnly }
            }
        );

        var tags = new List<Tag>
        {
            TagOf("Mild"),
            TagOf("Violence"),
            TagOf("Graphic")
        };

        var rating = MetadataService.GetAgeRating(preferences, tags);

        Assert.Equal(AgeRating.AdultsOnly, rating);
    }

    #endregion
}
