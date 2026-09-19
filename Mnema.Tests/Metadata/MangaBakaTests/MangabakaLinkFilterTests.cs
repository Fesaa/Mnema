using System;
using Mnema.Metadata.Mangabaka;
using Mnema.Models.Entities;
using Mnema.Models.Entities.User;
using Mnema.Models.Enums;

namespace Mnema.Tests.Metadata.MangaBakaTests;

public class MangabakaLinkFilterTests
{
    [Theory]
    [InlineData("mangaupdates.com")]
    [InlineData("www.mangaupdates.com")]
    [InlineData("MangaUpdates.com")]
    public void CollectLinks_FiltersSynthesizedLinkByHostname(string filterValue)
    {
        var preferences = CreatePreferences(
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Hostname, filterValue));
        var series = CreateSeries("en", Link("https://example.com/en", "en"));
        series.SourceMangaUpdatesId = "abc";

        var links = MangabakaMetadataService.CollectLinks(series, preferences);

        Assert.Single(links);
        Assert.Equal("https://example.com/en", links[0]);
    }

    [Fact]
    public void CollectLinks_DoesNotResolveNativePlaceholderIntoPreferences()
    {
        var preferences = CreatePreferences(
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Language, "{Native}"));
        var series = CreateSeries("ja", Link("https://example.com/ja", "ja"));

        var links = MangabakaMetadataService.CollectLinks(series, preferences);

        Assert.Empty(links);

        // The preference is shared with every other series, so it must keep the placeholder
        Assert.Equal("{Native}", preferences.LinkFilters[0].Value);
    }

    [Fact]
    public void CollectLinks_ResolvesNativePlaceholderPerSeries()
    {
        var preferences = CreatePreferences(
            new LinkFilter(LinkFilterMode.Exclude, LinkFilterType.Language, "{Native}"));

        var japanese = CreateSeries("ja", Link("https://example.com/ja", "ja"));
        var english = CreateSeries("en", Link("https://example.com/en", "en"));

        // {Native} means the language of the series being filtered, so both native links are excluded.
        // When the resolved value leaked back into the preference, the second series was still filtered
        // using "ja" and its English link was kept.
        Assert.Empty(MangabakaMetadataService.CollectLinks(japanese, preferences));
        Assert.Empty(MangabakaMetadataService.CollectLinks(english, preferences));
    }

    private static MangabakaLinkV2 Link(string url, string language)
    {
        return new MangabakaLinkV2
        {
            Url = url,
            Language = language,
        };
    }

    private static MangabakaSeries CreateSeries(string nativeLanguage, params MangabakaLinkV2[] links)
    {
        return new MangabakaSeries
        {
            Id = 1,
            Titles =
            [
                new MangabakaTitle
                {
                    Title = "Example Series",
                    Language = nativeLanguage,
                    Traits = ["native"],
                }
            ],
            LinksV2 = [.. links],
        };
    }

    private static Preferences CreatePreferences(params LinkFilter[] filters)
    {
        return new Preferences
        {
            Id = Guid.NewGuid(),
            ImageFormat = ImageFormat.Upstream,
            CoverFallbackMethod = CoverFallbackMethod.None,
            BlackListedTags = [],
            WhiteListedTags = [],
            AgeRatingMappings = [],
            MetadataFieldMappings = [],
            ConvertToGenreList = [],
            TagMappings = [],
            PinSubscriptionTitles = false,
            ChapterFileFormat = string.Empty,
            OneShotFileFormat = string.Empty,
            LinkFilters = filters,
        };
    }
}
