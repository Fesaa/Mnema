using System.Collections.Generic;
using System.Threading.Tasks;
using Mnema.API.Content;
using Mnema.Models.DTOs.Content;

namespace Mnema.Providers;

public class NoOpContentManager: IContentManager
{
    public Task Download(DownloadRequestDto request)
    {
        return Task.CompletedTask;
    }

    public Task StopDownload(StopRequestDto request)
    {
        return Task.CompletedTask;
    }

    public Task MoveToDownloadQueue(string id)
    {
        return Task.CompletedTask;
    }

    public Task<IEnumerable<IContent>> GetAllContent()
    {
        return Task.FromResult<IEnumerable<IContent>>([]);
    }

    public Task<MessageDto> RelayMessage(MessageDto message)
    {
        return Task.FromResult(new MessageDto
        {
            Provider = message.Provider,
            ContentId = message.ContentId,
            Type = message.Type,
        });
    }
}