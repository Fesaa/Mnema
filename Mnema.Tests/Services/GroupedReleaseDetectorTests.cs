using Mnema.Models.Enums;
using Mnema.Services;

namespace Mnema.Tests.Services;

public class GroupedReleaseDetectorTests
{

    [Theory]
    [InlineData("Weekly Viz and ShonenJump Chapter Updates - Week 30+31 2026 (Digital) (Rillant)", true)]
    [InlineData("(Partial) Weekly K Manga Chapter Updates - Week 31 2026 (Digital) (The K-Team)", true)]
    [InlineData("Monthly Viz Manga & Shonen Jump Volumes Update - July 2026 (Digital) (Rillant)", true)]
    [InlineData("Weekly Alpha Manga Chapter Updates - Week 30 2026 (Digital) (Anon)", true)]
    [InlineData("Weekly Manga UP! Chapter Updates - Week 30 2026 (Digital) (Oak)", true)]
    public void NyaaGroupedReleasesTest(string input, bool expected)
    {
        var detector = new GroupedReleaseDetector();
        Assert.Equal(expected, detector.IsGroupedRelease(Provider.Nyaa, input));
    }

}
