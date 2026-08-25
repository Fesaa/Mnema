using Mnema.Models.External;

namespace Mnema.Models.Publication;

public class OnDiskContent: IHasPositionMarkers
{
    public string SeriesName { get; set; }
    public string Path { get; set; }
    public string FileName { get; set; }
    public string? Chapter { get; set; }
    public string? Volume { get; set; }
    public ComicInfo? ComicInfo { get; set; }

    public string VolumeMarker => Volume ?? string.Empty;
    public string ChapterMarker => Chapter ?? string.Empty;
}
