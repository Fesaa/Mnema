namespace Mnema.Models.DTOs.Content;

public enum ContentState
{
    Queued = 0,
    Loading = 1,
    Waiting = 2,
    Ready = 3,
    Downloading = 4,
    Cleanup = 5,
    /// <summary>
    /// A special internal state to indicate, the publication has cancelled itself and does not require cleanup
    /// </summary>
    Cancel = 6,
}