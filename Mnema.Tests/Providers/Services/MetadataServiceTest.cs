using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Entities.User;
using Mnema.Models.Enums;
using Mnema.Models.External;
using Mnema.Models.Publication;
using Mnema.Providers.Services;
using Mnema.Services;
using NSubstitute;

namespace Mnema.Tests.Providers.Services;

[TestSubject(typeof(MetadataService))]
public class MetadataServiceTest
{

    private readonly IMetadataService _metadataService = new MetadataService(
        Substitute.For<ILogger<MetadataService>>(),
        Substitute.For<IEpubMetadataService>(),
        Substitute.For<IParserService>(),
        Substitute.For<IFileSystem>()
    );

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
            PinSubscriptionTitles = false,
            ChapterFileFormat = string.Empty,
            OneShotFileFormat = string.Empty,
            LinkFilters = [],
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

        var rating = _metadataService.GetAgeRating(preferences, tags);

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

        var rating = _metadataService.GetAgeRating(preferences, tags);

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

        var rating = _metadataService.GetAgeRating(preferences, tags);

        Assert.Null(rating);
    }

    [Fact]
    public void GetAgeRating_Returns_Null_When_No_Mappings_Configured()
    {
        var preferences = CreateDefaultPreferences(ageRatings: new List<AgeRatingMappingDto>());

        var tags = new List<Tag> { TagOf("Violence") };

        var rating = _metadataService.GetAgeRating(preferences, tags);

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

        var rating = _metadataService.GetAgeRating(preferences, tags);

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

        var rating = _metadataService.GetAgeRating(preferences, tags);

        Assert.Equal(AgeRating.AdultsOnly, rating);
    }

    #endregion

    #region CollectLinks (ComicInfo.Web) Tests

    private static Preferences CreatePreferencesWithFilters(params LinkFilter[] filters)
    {
        var preferences = CreateDefaultPreferences();
        preferences.LinkFilters = filters;

        return preferences;
    }

    private static DownloadRequestDto CreateRequest()
    {
        return new DownloadRequestDto
        {
            Provider = Provider.Mangadex,
            Id = "series-id",
            BaseDir = "Manga",
            TempTitle = "Example Series",
            Metadata = new MetadataBag(),
        };
    }

    private static Series CreateSeries(params string[] links)
    {
        return new Series
        {
            Id = "series-id",
            Title = "Example Series",
            Summary = string.Empty,
            Status = PublicationStatus.Ongoing,
            Tags = [],
            People = [],
            Links = links,
            Chapters = [],
        };
    }

    private ComicInfo? CreateComicInfo(Preferences preferences, Series series)
    {
        return _metadataService.CreateComicInfo(preferences, CreateRequest(), series.Title, series, null);
    }

    [Fact]
    public void CreateComicInfo_ExcludesLinkMatchingHostnameFilter()
    {
        var preferences = CreatePreferencesWithFilters(
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "mangaupdates.com"));
        var series = CreateSeries("https://mangaupdates.com/series/abc", "https://anilist.co/manga/123");

        var ci = CreateComicInfo(preferences, series);

        Assert.Equal("https://anilist.co/manga/123", ci!.Web);
    }

    [Fact]
    public void CreateComicInfo_ExcludesWwwLinkWithMixedCaseFilter()
    {
        var preferences = CreatePreferencesWithFilters(
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "MangaUpdates.com"));
        var series = CreateSeries("https://www.mangaupdates.com/series/abc", "https://anilist.co/manga/123");

        var ci = CreateComicInfo(preferences, series);

        Assert.Equal("https://anilist.co/manga/123", ci!.Web);
    }

    [Fact]
    public void CreateComicInfo_ExcludesSeriesRefUrl()
    {
        var preferences = CreatePreferencesWithFilters(
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "mangabaka.org"));
        var series = CreateSeries("https://anilist.co/manga/123");
        series.RefUrl = "https://mangabaka.org/123";

        var ci = CreateComicInfo(preferences, series);

        Assert.Equal("https://anilist.co/manga/123", ci!.Web);
    }

    [Fact]
    public void CreateComicInfo_KeepsLinksWhenNoFilterMatches()
    {
        var preferences = CreatePreferencesWithFilters(
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "blocked.com"));
        var series = CreateSeries("https://anilist.co/manga/123");

        var ci = CreateComicInfo(preferences, series);

        Assert.Equal("https://anilist.co/manga/123", ci!.Web);
    }

    [Fact]
    public void CreateComicInfo_IgnoresLanguageFilters()
    {
        // Web links carry no language, so language filters cannot apply to them
        var preferences = CreatePreferencesWithFilters(
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Language, "jp"));
        var series = CreateSeries("https://anilist.co/manga/123");

        var ci = CreateComicInfo(preferences, series);

        Assert.Equal("https://anilist.co/manga/123", ci!.Web);
    }

    [Fact]
    public void CreateComicInfo_EmptyWebWhenAllLinksAreFiltered()
    {
        var preferences = CreatePreferencesWithFilters(
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, "anilist.co"));
        var series = CreateSeries("https://anilist.co/manga/123");

        var ci = CreateComicInfo(preferences, series);

        Assert.Equal(string.Empty, ci!.Web);
    }

    #endregion
}
