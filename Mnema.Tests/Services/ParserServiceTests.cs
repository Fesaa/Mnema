using System.Collections.Generic;
using Mnema.API.Content;
using Mnema.Models.Enums;
using Mnema.Models.Publication;
using Mnema.Services;

namespace Mnema.Tests.Services;

/// <summary>
/// This class should only contain test cases for which Mnema difference from Kavita. The rest is tested in Kavita
/// </summary>
public class ParserServiceTests
{

    private readonly IParserService _parserService = new ParserService();

    [Theory]
    [InlineData(" Escape from the Seventh Night 001-005 as v01 (Digital-Compilation) (Oak)", "Escape from the Seventh Night")]
    [InlineData("The Executioner and Her Way of Life [Yen Press] [Stick]", "The Executioner and Her Way of Life")]
    [InlineData("The Executioner and Her Way of Life [Yen Press] [Stick].epub", "The Executioner and Her Way of Life")]
    [InlineData("The Long Summer of August.rar", "The Long Summer of August")]
    [InlineData("Hippos_the_Great [Digital]", "Hippos the Great")]
    [InlineData("[Yen Press] [Stick]", "")]
    public void ParserMangaSeries(string input, string expected)
    {
        var parsedSeries = _parserService.ParseSeries(input, ContentFormat.Manga);
        Assert.Equal(expected, parsedSeries);
    }

    [Fact]
    public void FindMatch_MatchesChapterInRange()
    {
        var file = new OnDiskContent { Volume = "1", Chapter = "1-10" };
        var chapter = CreateChapter("1", "5");

        var match = _parserService.FindMatch(new List<OnDiskContent> { file }, chapter);

        Assert.Same(file, match);
    }

    [Fact]
    public void FindMatch_DoesNotMatchChapterOutsideRange()
    {
        var file = new OnDiskContent { Volume = "1", Chapter = "1-10" };
        var chapter = CreateChapter("1", "11");

        var match = _parserService.FindMatch(new List<OnDiskContent> { file }, chapter);

        Assert.Null(match);
    }

    [Fact]
    public void FindMatch_DoesNotMatchRangeFromAnotherVolume()
    {
        var file = new OnDiskContent { Volume = "1", Chapter = "1-10" };
        var chapter = CreateChapter("2", "5");

        var match = _parserService.FindMatch(new List<OnDiskContent> { file }, chapter);

        Assert.Null(match);
    }

    [Fact]
    public void FindMatch_MatchesVolumeFileForEveryChapter()
    {
        var file = new OnDiskContent { Volume = "1", Chapter = string.Empty };
        var chapter = CreateChapter("1", "5");

        var match = _parserService.FindMatch(new List<OnDiskContent> { file }, chapter);

        Assert.Same(file, match);
    }

    [Fact]
    public void FindMatch_MatchesFileWithVolumeMarker_ForLooseLeafChapter()
    {
        var file = new OnDiskContent { Volume = "4", Chapter = "16.5" };
        var chapter = CreateChapter(string.Empty, "16.5");

        var match = _parserService.FindMatch(new List<OnDiskContent> { file }, chapter);

        Assert.Same(file, match);
    }

    [Fact]
    public void FindMatch_MatchesLooseLeafFile_ForChapterWithVolume()
    {
        var file = new OnDiskContent { Volume = string.Empty, Chapter = "16.5" };
        var chapter = CreateChapter("4", "16.5");

        var match = _parserService.FindMatch(new List<OnDiskContent> { file }, chapter);

        Assert.Same(file, match);
    }

    [Fact]
    public void FindMatch_PrefersLooseLeafChapter_OverPartialVolumeMatch()
    {
        var looseLeaf = new OnDiskContent { Volume = string.Empty, Chapter = "16.5" };
        var otherChapterInVolume = new OnDiskContent { Volume = "4", Chapter = "16.4" };
        var chapter = CreateChapter("4", "16.5");

        var match = _parserService.FindMatch(new List<OnDiskContent> { otherChapterInVolume, looseLeaf }, chapter);

        Assert.Same(looseLeaf, match);
    }

    [Fact]
    public void FindMatch_DoesNotMatchDifferentNonEmptyVolumes()
    {
        var file = new OnDiskContent { Volume = "5", Chapter = "16.5" };
        var chapter = CreateChapter("4", "16.5");

        var match = _parserService.FindMatch(new List<OnDiskContent> { file }, chapter);

        Assert.Null(match);
    }

    [Fact]
    public void FindMatch_DoesNotMatchAmbiguousVolumeMarkers_ForLooseLeafChapter()
    {
        var files = new List<OnDiskContent>
        {
            new() { Volume = "4", Chapter = "16.5" },
            new() { Volume = "5", Chapter = "16.5" },
        };
        var chapter = CreateChapter(string.Empty, "16.5");

        var match = _parserService.FindMatch(files, chapter);

        Assert.Null(match);
    }

    [Fact]
    public void FindMatch_DoesNotMatchVolumeMarkers_WhenPartialVolumeMatchDisallowed()
    {
        var file = new OnDiskContent { Volume = "4", Chapter = "16.5" };
        var chapter = CreateChapter(string.Empty, "16.5");

        var match = _parserService.FindMatch(new List<OnDiskContent> { file }, chapter, allowPartialVolumeMatch: false);

        Assert.Null(match);
    }

    private static Chapter CreateChapter(string volume, string chapter)
    {
        return new Chapter
        {
            Id = "1",
            Title = string.Empty,
            VolumeMarker = volume,
            ChapterMarker = chapter,
        };
    }
}
