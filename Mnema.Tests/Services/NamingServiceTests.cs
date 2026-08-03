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

    private readonly Preferences _preferences = new()
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
        OneShotFileFormat = string.Empty
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

        var result = _namingService.GetChapterFileName(_preferences, "Spice and Wolf", chpt);
        Assert.Equal(expected, result);
    }

}
