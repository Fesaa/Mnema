using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Models.Entities;
using Mnema.Models.Entities.User;
using Mnema.Models.Internal;
using Mnema.Models.Publication;
using Mnema.Services;
using NSubstitute;

namespace Mnema.Tests.Services;

public class NamingServiceTests
{

    private readonly INamingService _namingService = new NamingService(
        Substitute.For<ILogger<NamingService>>(),
        new ApplicationConfiguration { },
        new ParserService()
    );

    private Preferences Preferences => new()
    {
        ImageFormat = ImageFormat.Upstream,
        CoverFallbackMethod = CoverFallbackMethod.First,
        BlackListedTags = [],
        WhiteListedTags = [],
        AgeRatingMappings = [],
        MetadataFieldMappings = [],
        ConvertToGenreList = [],
        TagMappings = [],
        PinSubscriptionTitles = false,
        ChapterFileFormat = string.Empty,
        OneShotFileFormat = string.Empty,
        LinkFilters = [],
    };

    [Theory]
    [InlineData("1", "1", "Spice and Wolf Vol. 1 Ch. 0001")]
    [InlineData("1", "", "Spice and Wolf Vol. 1")]
    [InlineData("", "1", "Spice and Wolf Ch. 0001")]
    public void TestDefaultChapterFormatting(string volume, string chapter, string expected)
    {
        var chpt = new Chapter
        {
            Id = string.Empty,
            Title = string.Empty,
            VolumeMarker = volume,
            ChapterMarker = chapter
        };

        var result = _namingService.GetChapterFileName(Preferences, "Spice and Wolf", chpt);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TestChapterTitleFormatting()
    {
        var chpt = new Chapter
        {
            Id = string.Empty,
            Title = "The Beginning",
            VolumeMarker = "1",
            ChapterMarker = "2"
        };

        var pref = Preferences;
        pref.ChapterFileFormat = "{Title}[ Vol. {Volume}][ Ch. {Chapter:#4}][ {ChapterTitle}]";

        var result = _namingService.GetChapterFileName(pref, "Spice and Wolf", chpt);

        Assert.Equal("Spice and Wolf Vol. 1 Ch. 0002 The Beginning", result);
    }

}
