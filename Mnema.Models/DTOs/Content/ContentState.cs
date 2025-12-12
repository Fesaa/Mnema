namespace Mnema.Models.DTOs.Content;

public enum ContentState
{
    Queued = 0,
    Loading = 1,
    Waiting = 2,
    Ready = 3,
    Downloading = 4,
    Cleanup = 5,
}