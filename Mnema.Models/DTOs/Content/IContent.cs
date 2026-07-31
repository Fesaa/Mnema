namespace Mnema.Models.DTOs.Content;

public interface IContent
{
    string Id { get; }

    string Title { get; }

    string DownloadDir { get; }

    ContentState State { get; }

    DownloadRequestDto Request { get; }

    DownloadInfo DownloadInfo { get; }
}
