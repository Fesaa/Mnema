namespace Mnema.Tests.Metadata.MangaBakaTests;

using System.Collections.Generic;
using Mnema.Metadata.Mangabaka;
using Xunit;

public class MangabakaTitlePriorityTests
{
    [Fact]
    public void FindTitleByPriority_NullOrEmptyTitles_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, MangabakaMetadataService.FindTitleByPriority(null, "en,{SL}"));
        Assert.Equal(string.Empty, MangabakaMetadataService.FindTitleByPriority(new List<MangabakaTitle>(), "en,{SL}"));
    }

    [Fact]
    public void FindTitleByPriority_NullOrWhitespaceSetting_FallsBackToFindBestTitle()
    {
        var titles = new List<MangabakaTitle>
        {
            CreateTitle("Spice and Wolf", "en", isPrimary: true),
            CreateTitle("狼と香辛料", "ja", traits: ["native"])
        };

        var resultNull = MangabakaMetadataService.FindTitleByPriority(titles, null);
        var resultWhitespace = MangabakaMetadataService.FindTitleByPriority(titles, "   ");

        Assert.Equal("Spice and Wolf", resultNull);
        Assert.Equal("Spice and Wolf", resultWhitespace);
    }

    [Fact]
    public void FindTitleByPriority_MatchesFirstAvailableLanguageInPriorityList()
    {
        var titles = new List<MangabakaTitle>
        {
            CreateTitle("Spice and Wolf (FR)", "fr"),
            CreateTitle("Spice and Wolf (EN)", "en"),
            CreateTitle("Spice and Wolf (JA)", "ja")
        };

        // Setting requests French first, then English
        var result = MangabakaMetadataService.FindTitleByPriority(titles, "fr, en, ja");

        Assert.Equal("Spice and Wolf (FR)", result);
    }

    [Fact]
    public void FindTitleByPriority_PrimaryTitleWinsWhenSameLanguageMatches()
    {
        var titles = new List<MangabakaTitle>
        {
            CreateTitle("Secondary EN Title", "en", isPrimary: false),
            CreateTitle("Primary EN Title", "en", isPrimary: true)
        };

        var result = MangabakaMetadataService.FindTitleByPriority(titles, "en");

        Assert.Equal("Primary EN Title", result);
    }

    [Fact]
    public void FindTitleByPriority_ResolvesNativeLanguagePlaceholder()
    {
        var titles = new List<MangabakaTitle>
        {
            CreateTitle("Spice and Wolf (EN)", "en"),
            CreateTitle("狼と香辛料", "ja", traits: ["native"])
        };

        // {SL} should resolve to "ja" because of the "native" trait
        var result = MangabakaMetadataService.FindTitleByPriority(titles, "{SL}, en");

        Assert.Equal("狼と香辛料", result);
    }

    [Fact]
    public void FindTitleByPriority_NoMatchingPriority_ReturnsFindBestTitleWhenNotLocalized()
    {
        var titles = new List<MangabakaTitle>
        {
            CreateTitle("Spice and Wolf (EN)", "en", isPrimary: true),
            CreateTitle("狼と香辛料", "ja", traits: ["native"])
        };

        // Priority requests German or Spanish, neither exists in titles
        var result = MangabakaMetadataService.FindTitleByPriority(titles, "de, es", isLocalized: false);

        Assert.Equal("Spice and Wolf (EN)", result);
    }

    [Fact]
    public void FindTitleByPriority_NoMatchingPriority_ReturnsFindBestNativeTitleWhenIsLocalizedIsTrue()
    {
        var titles = new List<MangabakaTitle>
        {
            CreateTitle("Spice and Wolf (EN)", "en", isPrimary: true),
            CreateTitle("狼と香辛料", "ja", traits: ["native"])
        };

        // Priority requests German or Spanish, neither exists in titles
        var result = MangabakaMetadataService.FindTitleByPriority(titles, "de, es", isLocalized: true);

        Assert.Equal("狼と香辛料", result);
    }

    [Fact]
    public void FindTitleByPriority_CaseInsensitiveLanguageMatching()
    {
        var titles = new List<MangabakaTitle>
        {
            CreateTitle("Spice and Wolf (JA-LATN)", "ja-Latn")
        };

        var result = MangabakaMetadataService.FindTitleByPriority(titles, "JA-LATN");

        Assert.Equal("Spice and Wolf (JA-LATN)", result);
    }

    [Fact]
    public void FindTitleByPriority_NativeResolution_NonLatn()
    {
        var titles = new List<MangabakaTitle>
        {
            CreateTitle("Spice and Wolf (JA-LATN)", "ja-Latn", traits: ["native"]),
            CreateTitle("Spice and Wolf (JA)", "ja", traits: ["native"]),
            CreateTitle("Spice and Wolf (EN)", "en")
        };

        var result = MangabakaMetadataService.FindTitleByPriority(titles, "{SL},en,{SL}-Latn");

        Assert.Equal("Spice and Wolf (JA)", result);
    }

    [Fact]
    public void FindTitleByPriority_NativeResolution_Latn()
    {
        var titles = new List<MangabakaTitle>
        {
            CreateTitle("Spice and Wolf (JA-LATN)", "ja-Latn", traits: ["native"]),
            CreateTitle("Spice and Wolf (EN)", "en")
        };

        var result = MangabakaMetadataService.FindTitleByPriority(titles, "{SL}-Latn,en,{SL}");

        Assert.Equal("Spice and Wolf (JA-LATN)", result);
    }

    private static MangabakaTitle CreateTitle(
        string title,
        string language,
        bool isPrimary = false,
        List<string>? traits = null)
    {
        return new MangabakaTitle
        {
            Title = title,
            Language = language,
            IsPrimary = isPrimary,
            Traits = traits ?? []
        };
    }
}
