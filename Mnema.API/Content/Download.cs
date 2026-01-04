using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;

namespace Mnema.API.Content;

public interface IDownloadService
{
    Task StartDownload(DownloadRequestDto request);

    Task CancelDownload(StopRequestDto request);

    Task<IList<DownloadInfo>> GetCurrentContent(Guid userId);
}

public interface IContent
{
    string Id { get; }

    string Title { get; }

    string DownloadDir { get; }

    ContentState State { get; }

    DownloadRequestDto Request { get; }

    DownloadInfo DownloadInfo { get; }

    Task Cancel();

    Task Cleanup();

    Task<MessageDto> ProcessMessage(MessageDto message);
}

/// <summary>
///     A manger that holds all content off the same type (Publication custom / torrent)
/// </summary>
public interface IContentManager
{
    Task Download(DownloadRequestDto request);
    Task StopDownload(StopRequestDto request);
    Task MoveToDownloadQueue(string id);
    Task<IEnumerable<IContent>> GetAllContent();
    Task<MessageDto> RelayMessage(MessageDto message);
}