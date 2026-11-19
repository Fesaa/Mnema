using System.Collections.Generic;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Providers.Cleanup;
using Mnema.Services;

namespace Mnema.Tests.Providers.Cleanup;

public class RawFileCleanupServiceTests
{
    [Fact]
    public void ResolveMarkers_UsesStoredMarkers()
    {
        var markers = RawFileCleanupService.ResolveMarkers(CreateDownloadFile("2", "7"));

        Assert.Equal("2", markers.VolumeMarker);
        Assert.Equal("7", markers.ChapterMarker);
    }

    [Fact]
    public void ResolveMarkers_NullMarkersBecomeEmpty()
    {
        var markers = RawFileCleanupService.ResolveMarkers(CreateDownloadFile(null, null));

        Assert.Equal(string.Empty, markers.VolumeMarker);
        Assert.Equal(string.Empty, markers.ChapterMarker);
    }

    [Fact]
    public void ResolveMarkers_OverrideMatchesTheIntendedChapter()
    {
        var chapters = new List<Chapter>
        {
            new() { Id = "1", Title = string.Empty, VolumeMarker = "1", ChapterMarker = "7" },
            new() { Id = "2", Title = string.Empty, VolumeMarker = "2", ChapterMarker = "7" },
        };

        var match = new ParserService().FindMatch(chapters,
            RawFileCleanupService.ResolveMarkers(CreateDownloadFile("2", "7")));

        Assert.NotNull(match);
        Assert.Equal("2", match.VolumeMarker);
    }

    private static DownloadFile CreateDownloadFile(string? volumeMarker, string? chapterMarker) => new()
    {
        FileName = "Spice and Wolf v01.cbz",
        FullPath = "Spice and Wolf v01.cbz",
        FileSize = 1,
        VolumeMarker = volumeMarker,
        ChapterMarker = chapterMarker,
        Selected = true,
    };
}
