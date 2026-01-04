using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;
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
            DownloadMetadata = new DownloadMetadataDto
            {
                Extra = new MetadataBag
                {
                    [RequestConstants.IncludeNotMatchedTagsKey] = ["true"]
                }
            }
        };
    }

    private static UserPreferences CreateDefaultPreferences(
        IList<TagMappingDto>? tagMappings = null,
        IList<AgeRatingMappingDto>? ageRatings = null,
        IList<string>? genres = null,
        IList<string>? blacklist = null,
        IList<string>? whitelist = null)
    {
        return new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = null!,
            ImageFormat = ImageFormat.Upstream,
            CoverFallbackMethod = CoverFallbackMethod.None,
            ConvertToGenreList = genres ?? [],
            BlackListedTags = blacklist ?? [],
            WhiteListedTags = whitelist ?? [],
            AgeRatingMappings = ageRatings ?? [],
            TagMappings = tagMappings ?? [],
            PinSubscriptionTitles = false
        };
    }

    private static Tag TagOf(string value)
    {
        return new Tag { Value = value };
    }

    #region MapTags Tests

    [Fact]
    public void MapTags_Replaces_Origin_With_Destination_Using_Normalized_Matching()
    {
        var sut = CreateSut();

        var tags = new List<Tag>
        {
            TagOf("Violence"),
            TagOf(" Romance ")
        };

        var mappings = new List<TagMappingDto>
        {
            new() { OriginTag = "violence", DestinationTag = "action" }
        };

        var result = sut.MapTags(tags, mappings);

        Assert.Contains(result, t => t.Value == "action");
        Assert.DoesNotContain(result, t => t.Value.Equals("Violence", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, t => t.Value.Trim() == "Romance");
    }

    [Fact]
    public void MapTags_Returns_Empty_List_When_Input_Is_Empty()
    {
        var sut = CreateSut();

        var tags = new List<Tag>();
        var mappings = new List<TagMappingDto>();

        var result = sut.MapTags(tags, mappings);

        Assert.Empty(result);
    }

    [Fact]
    public void MapTags_Returns_Original_Tags_When_No_Mappings_Provided()
    {
        var sut = CreateSut();

        var tags = new List<Tag>
        {
            TagOf("Action"),
            TagOf("Comedy")
        };
        var mappings = new List<TagMappingDto>();

        var result = sut.MapTags(tags, mappings);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Value == "Action");
        Assert.Contains(result, t => t.Value == "Comedy");
    }

    [Fact]
    public void MapTags_Applies_Multiple_Mappings_Correctly()
    {
        var sut = CreateSut();

        var tags = new List<Tag>
        {
            TagOf("SciFi"),
            TagOf("Rom-Com"),
            TagOf("Horror")
        };
        var mappings = new List<TagMappingDto>
        {
            new() { OriginTag = "scifi", DestinationTag = "Science Fiction" },
            new() { OriginTag = "rom-com", DestinationTag = "Romantic Comedy" }
        };

        var result = sut.MapTags(tags, mappings);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, t => t.Value == "Science Fiction");
        Assert.Contains(result, t => t.Value == "Romantic Comedy");
        Assert.Contains(result, t => t.Value == "Horror");
        Assert.DoesNotContain(result, t => t.Value.Equals("SciFi", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result, t => t.Value.Equals("Rom-Com", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapTags_Handles_Whitespace_In_Tag_Values()
    {
        var sut = CreateSut();

        var tags = new List<Tag>
        {
            TagOf("  Action  "),
            TagOf("Comedy")
        };
        var mappings = new List<TagMappingDto>
        {
            new() { OriginTag = "action", DestinationTag = "Adventure" }
        };

        var result = sut.MapTags(tags, mappings);

        Assert.Contains(result, t => t.Value == "Adventure");
        Assert.Contains(result, t => t.Value == "Comedy");
    }

    #endregion

    #region GetAgeRating Tests

    [Fact]
    public void GetAgeRating_Returns_Highest_Mapped_AgeRating()
    {
        var sut = CreateSut();

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

        var rating = sut.GetAgeRating(preferences, tags);

        Assert.Equal(AgeRating.Mature, rating);
    }

    [Fact]
    public void GetAgeRating_Returns_Null_When_No_Tags_Match()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            ageRatings: new List<AgeRatingMappingDto>
            {
                new() { Tag = "violence", AgeRating = AgeRating.Teen }
            }
        );

        var tags = new List<Tag> { TagOf("Romance") };

        var rating = sut.GetAgeRating(preferences, tags);

        Assert.Null(rating);
    }

    [Fact]
    public void GetAgeRating_Returns_Null_When_No_Input_Tags()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            ageRatings: new List<AgeRatingMappingDto>
            {
                new() { Tag = "violence", AgeRating = AgeRating.Teen }
            }
        );

        var tags = new List<Tag>();

        var rating = sut.GetAgeRating(preferences, tags);

        Assert.Null(rating);
    }

    [Fact]
    public void GetAgeRating_Returns_Null_When_No_Mappings_Configured()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(ageRatings: new List<AgeRatingMappingDto>());

        var tags = new List<Tag> { TagOf("Violence") };

        var rating = sut.GetAgeRating(preferences, tags);

        Assert.Null(rating);
    }

    [Fact]
    public void GetAgeRating_Uses_Normalized_Matching()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            ageRatings: new List<AgeRatingMappingDto>
            {
                new() { Tag = "VIOLENCE", AgeRating = AgeRating.Mature }
            }
        );

        var tags = new List<Tag> { TagOf("violence") };

        var rating = sut.GetAgeRating(preferences, tags);

        Assert.Equal(AgeRating.Mature, rating);
    }

    [Fact]
    public void GetAgeRating_Returns_Highest_Rating_From_Multiple_Matches()
    {
        var sut = CreateSut();

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

        var rating = sut.GetAgeRating(preferences, tags);

        Assert.Equal(AgeRating.AdultsOnly, rating);
    }

    #endregion

    #region ProcessTags Tests

    [Fact]
    public void ProcessTags_Maps_Tags_And_Removes_Blacklisted()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            new List<TagMappingDto>
            {
                new() { OriginTag = "violence", DestinationTag = "action" }
            },
            blacklist: new List<string> { "gore" }
        );

        var tags = new List<Tag>
        {
            TagOf("Violence"),
            TagOf("Gore"),
            TagOf("Romance")
        };

        var req = Request();
        req.DownloadMetadata.Extra[RequestConstants.IncludeNotMatchedTagsKey] = ["true"];
        var (_, processedTags) = sut.ProcessTags(preferences, tags, req);

        Assert.Contains("action", processedTags);
        Assert.DoesNotContain("gore", processedTags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Romance", processedTags);
    }

    [Fact]
    public void ProcessTags_Extracts_Genres_From_Configured_List()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            genres: new List<string> { "romance", "fantasy" }
        );

        var tags = new List<Tag>
        {
            TagOf("Romance"),
            TagOf("Drama")
        };

        var (genres, processedTags) =
            sut.ProcessTags(preferences, tags, Request());

        Assert.Single(genres);
        Assert.Equal("Romance", genres[0]);
        Assert.DoesNotContain("Romance", processedTags);
    }

    [Fact]
    public void ProcessTags_Allows_Whitelisted_Tags_Even_If_Unmapped()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            whitelist: new List<string> { "experimental" }
        );

        var tags = new List<Tag>
        {
            TagOf("Experimental")
        };

        var (_, processedTags) =
            sut.ProcessTags(preferences, tags, Request());

        Assert.Contains("Experimental", processedTags);
    }

    [Fact]
    public void ProcessTags_Returns_Empty_Lists_When_No_Input_Tags()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences();
        var tags = new List<Tag>();

        var (genres, processedTags) = sut.ProcessTags(preferences, tags, Request());

        Assert.Empty(genres);
        Assert.Empty(processedTags);
    }

    [Fact]
    public void ProcessTags_Extracts_Multiple_Genres()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            genres: new List<string> { "action", "romance", "fantasy" }
        );

        var tags = new List<Tag>
        {
            TagOf("Action"),
            TagOf("Romance"),
            TagOf("Drama"),
            TagOf("Fantasy")
        };

        var (genres, processedTags) = sut.ProcessTags(preferences, tags, Request());

        Assert.Equal(3, genres.Count);
        Assert.Contains("Action", genres);
        Assert.Contains("Romance", genres);
        Assert.Contains("Fantasy", genres);
        Assert.DoesNotContain("Action", processedTags);
        Assert.DoesNotContain("Romance", processedTags);
        Assert.DoesNotContain("Fantasy", processedTags);
        Assert.Contains("Drama", processedTags);
    }

    [Fact]
    public void ProcessTags_Filters_Multiple_Blacklisted_Tags()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            blacklist: new List<string> { "gore", "violence", "explicit" }
        );

        var tags = new List<Tag>
        {
            TagOf("Gore"),
            TagOf("Adventure"),
            TagOf("Violence"),
            TagOf("Comedy"),
            TagOf("Explicit")
        };

        var (_, processedTags) = sut.ProcessTags(preferences, tags, Request());

        Assert.DoesNotContain("Gore", processedTags, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Violence", processedTags, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Explicit", processedTags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Adventure", processedTags);
        Assert.Contains("Comedy", processedTags);
    }

    [Fact]
    public void ProcessTags_Whitelist_Filters_Out_Non_Whitelisted_Tags()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            whitelist: new List<string> { "adventure", "comedy" }
        );

        var tags = new List<Tag>
        {
            TagOf("Adventure"),
            TagOf("Horror"),
            TagOf("Comedy"),
            TagOf("Thriller")
        };

        var (_, processedTags) = sut.ProcessTags(preferences, tags, Request());

        Assert.Contains("Adventure", processedTags);
        Assert.Contains("Comedy", processedTags);
        Assert.DoesNotContain("Horror", processedTags);
        Assert.DoesNotContain("Thriller", processedTags);
    }

    [Fact]
    public void ProcessTags_Applies_Mappings_Before_Genre_Extraction()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            new List<TagMappingDto>
            {
                new() { OriginTag = "scifi", DestinationTag = "science fiction" }
            },
            genres: new List<string> { "science fiction" }
        );

        var tags = new List<Tag>
        {
            TagOf("SciFi"),
            TagOf("Adventure")
        };

        var (genres, processedTags) = sut.ProcessTags(preferences, tags, Request());

        Assert.Contains("science fiction", genres);
        Assert.DoesNotContain("science fiction", processedTags);
        Assert.Contains("Adventure", processedTags);
    }

    [Fact]
    public void ProcessTags_Combines_All_Rules_Correctly()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            new List<TagMappingDto>
            {
                new() { OriginTag = "scifi", DestinationTag = "science fiction" }
            },
            genres: new List<string> { "action", "science fiction" },
            blacklist: new List<string> { "gore" }
        );

        var tags = new List<Tag>
        {
            TagOf("Action"),
            TagOf("SciFi"),
            TagOf("Gore"),
            TagOf("Comedy")
        };

        var (genres, processedTags) = sut.ProcessTags(preferences, tags, Request());

        Assert.Equal(2, genres.Count);
        Assert.Contains("Action", genres);
        Assert.Contains("science fiction", genres);
        Assert.DoesNotContain("Gore", processedTags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Comedy", processedTags);
        Assert.DoesNotContain("Action", processedTags);
        Assert.DoesNotContain("science fiction", processedTags);
    }

    [Fact]
    public void ProcessTags_Uses_Normalized_Matching_For_All_Operations()
    {
        var sut = CreateSut();

        var preferences = CreateDefaultPreferences(
            genres: new List<string> { "ACTION" },
            blacklist: new List<string> { "GORE" }
        );

        var tags = new List<Tag>
        {
            TagOf("action"),
            TagOf("gore"),
            TagOf("Comedy")
        };

        var (genres, processedTags) = sut.ProcessTags(preferences, tags, Request());

        Assert.Contains("action", genres);
        Assert.DoesNotContain("gore", processedTags, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Comedy", processedTags);
    }

    #endregion
}