using System;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;

namespace Mnema.API.Content;

public interface IPublicationManager : IContentManager
{
    Task<IPublication?> GetPublicationById(string id);
    Task MoveToDownloadQueue(string id);
}

public interface IPublication : IContent, IAsyncDisposable
{
    Task Cancel();
    Task Cleanup();
    Task<MessageDto> ProcessMessage(MessageDto message);
    Task LoadMetadataAsync(CancellationTokenSource source);
    Task DownloadContentAsync(CancellationTokenSource source);
}
