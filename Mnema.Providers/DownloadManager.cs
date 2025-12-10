
using Mnema.API.Providers;
using Mnema.Models.DTOs.Content;

namespace Mnema.Providers;

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

    public Task<IPublication> GetPublicationById(string id)
    {
        throw new NotImplementedException();
    }
    
    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}