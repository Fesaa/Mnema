using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;

namespace Mnema.API.Content;

public interface IDownloadService
{
    Task StartDownload(DownloadRequestDto request);

    Task CancelDownload(StopRequestDto request);

    Task<IList<DownloadInfo>> GetCurrentContent();

    Task<bool> HasContent(Provider provider, string id);
}

/// <summary>
///     A manger that holds all content off the same type (Publication custom / torrent)
/// </summary>
public interface IContentManager
{
    Task Download(DownloadRequestDto request);
    Task StopDownload(StopRequestDto request);
    Task<bool> HasContent(Provider provider, string id);
    Task<IEnumerable<IContent>> GetAllContent(Provider provider);
    Task<MessageDto> RelayMessage(MessageDto message, CancellationToken ct = default);
}
