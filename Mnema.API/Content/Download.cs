using Mnema.Models.DTOs.Content;

namespace Mnema.API.Content;

public interface IDownloadService
{

    Task StartDownload(DownloadRequestDto request);

    Task CancelDownload(StopRequestDto request);

    Task<IList<DownloadInfo>> GetCurrentContent();
    
}

public interface IContent
{
    string Id { get; }
    
    string Title { get; }
    
    ContentState State { get; }
    
    DownloadInfo DownloadInfo { get; }

    Task Cancel();
}

/// <summary>
/// A manger that holds all content off the same type (Publication custom / torrent)
/// </summary>
public interface IContentManager
{
    string BaseDir { get; }

    Task Download(DownloadRequestDto request);
    Task StopDownload(StopRequestDto request);
    Task MoveToDownloadQueue(string id);
    Task<IEnumerable<IContent>> GetAllContent();
}