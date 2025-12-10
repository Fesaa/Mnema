
using Mnema.Models.DTOs.Content;

namespace Mnema.Providers;

public interface IDownloadManager: IAsyncDisposable
{
    string BaseDir { get; }

    Task Download(DownloadRequestDto request);
    Task StopDownload(StopRequestDto request);
    Task MoveToDownloadQueue(string id);
    Task<Publication> GetPublicationById(string id);

}

public sealed class DownloadManager: IDownloadManager
{
    public string BaseDir { get; private set; } = null!;

    public Task Download(DownloadRequestDto request)
    {
        throw new NotImplementedException();
    }

    public Task StopDownload(StopRequestDto request)
    {
        throw new NotImplementedException();
    }

    public Task MoveToDownloadQueue(string id)
    {
        throw new NotImplementedException();
    }

    public Task<Publication> GetPublicationById(string id)
    {
        throw new NotImplementedException();
    }
    
    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}