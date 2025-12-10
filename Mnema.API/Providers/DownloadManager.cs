using Mnema.Models.DTOs.Content;

namespace Mnema.API.Providers;

public interface IDownloadManager: IAsyncDisposable
{
    string BaseDir { get; }

    Task Download(DownloadRequestDto request);
    Task StopDownload(StopRequestDto request);
    Task MoveToDownloadQueue(string id);
    Task<IPublication> GetPublicationById(string id);

}