namespace Mnema.Models.Publication;

public enum PublicationState
{
    Queued = 0,
    Loading = 1,
    Waiting = 2,
    Ready = 3,
    Downloading = 4,
    Cleanup = 5,
}