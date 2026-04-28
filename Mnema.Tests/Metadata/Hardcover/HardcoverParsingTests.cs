using Mnema.Metadata.Hardcover;

namespace Mnema.Tests.Metadata.Hardcover;

public class HardcoverParsingTests
{

    [Theory]
    [InlineData("I Who Have Never Known Men", null, "I Who Have Never Known Men")]
    [InlineData("Spice and Wolf, Vol. 15 (light novel): The Coin of the Sun I", 15.0f, "The Coin of the Sun I")]
    [InlineData("Buying a Classmate Once a Week, Vol. 1 (Light Novel): Our Time Together and the Five-Thousand-Yen Excuse", 1.0f, "Our Time Together and the Five-Thousand-Yen Excuse")]
    public void TestChapterSubtitleParsing(string chapterTitle, float? position, string expectedTitle)
    {
        Assert.Equal(expectedTitle, HardcoverMetadataService.ParseChapterTitle(chapterTitle, position));
    }

}
