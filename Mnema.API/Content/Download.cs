using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.API.Content;

public interface IDownloadService
{
    Task StartDownload(DownloadRequestDto request);

    Task CancelDownload(StopRequestDto request);

    Task<IList<DownloadInfo>> GetCurrentContent(Guid userId);

    Task<bool> HasContent(Provider provider, string id);
}

public interface IContent
{
    string Id { get; }

    string Title { get; }

    string DownloadDir { get; }

    ContentState State { get; }

    DownloadRequestDto Request { get; }

    DownloadInfo DownloadInfo { get; }
}

/// <summary>
///     A manger that holds all content off the same type (Publication custom / torrent)
/// </summary>
public interface IContentManager
{
    Task Download(DownloadRequestDto request);
    Task StopDownload(StopRequestDto request);
    Task MoveToDownloadQueue(string id);
    Task<bool> HasContent(Provider provider, string id);
    Task<IEnumerable<IContent>> GetAllContent(Provider provider);
    Task<MessageDto> RelayMessage(MessageDto message);
}
