namespace Mnema.Models.Publication;

public interface IHasPositionMarkers
{
    string VolumeMarker { get; }
    string ChapterMarker { get; }
}
