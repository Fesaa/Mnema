using System.Collections.Generic;
using System.Threading.Tasks;
using Mnema.API.Content;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Providers.Managers.DDL;

public class DirectDownloadManager: IContentManager
{
    public Task Download(DownloadRequestDto request)
    {
        throw new System.NotImplementedException();
    }

    public Task StopDownload(StopRequestDto request)
    {
        throw new System.NotImplementedException();
    }

    public Task MoveToDownloadQueue(string id)
    {
        throw new System.NotImplementedException();
    }

    public Task<IEnumerable<IContent>> GetAllContent(Provider provider)
    {
        throw new System.NotImplementedException();
    }

    public Task<MessageDto> RelayMessage(MessageDto message)
    {
        throw new System.NotImplementedException();
    }
}
