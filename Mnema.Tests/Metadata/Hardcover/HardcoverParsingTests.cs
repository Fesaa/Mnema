using System.Globalization;
using Mnema.Common;
using Mnema.Metadata.Hardcover;
using Mnema.Models.Entities;

namespace Mnema.Tests.Metadata.Hardcover;

public class HardcoverParsingTests
{

    [Theory]
    [InlineData("Spice and Wolf, Vol. 15 (light novel): The Coin of the Sun I", 15.0f, "The Coin of the Sun I")]
    [InlineData("Buying a Classmate Once a Week, Vol. 1 (Light Novel): Our Time Together and the Five-Thousand-Yen Excuse", 1.0f, "Our Time Together and the Five-Thousand-Yen Excuse")]
    public void TestChapterSubtitleParsing(string chapterTitle, float? position, string expectedTitle)
    {
        Assert.Equal(expectedTitle, HardcoverMetadataService.ParseChapterTitle(new MetadataProviderSettings()
        {
            MetadataProviderSpecific = new MetadataBag()
        }, chapterTitle, position));
    }

    [Fact]
    public void FormatVolumeMarker_UsesInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("8.1", HardcoverMetadataService.FormatVolumeMarker(8.1f));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void ParseChapterTitle_UsesInvariantVolumeMarker()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var title = HardcoverMetadataService.ParseChapterTitle(new MetadataProviderSettings
            {
                MetadataProviderSpecific = new MetadataBag()
            }, "Spice and Wolf, Vol. 8.1: More Stories", 8.1f);

            Assert.Equal("More Stories", title);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

}
