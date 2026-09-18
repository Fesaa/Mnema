using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Enums;

namespace Mnema.Providers.Managers.Dropped;

public class DroppedContentManager(IUnitOfWork unitOfWork): IContentManager
{
    public Task Download(DownloadRequestDto request)
    {
        throw new BadRequestException("Dropped content is not start via the download method");
    }

    public Task StopDownload(StopRequestDto request)
    {
        // TODO: Hook into Hangfire to cancel the task?
        throw new NotImplementedException();
    }

    public Task<bool> HasContent(Provider provider, string id)
    {
        return unitOfWork.DroppedContentRepository.Exists(Guid.Parse(id));
    }

    public async Task<IEnumerable<IContent>> GetAllContent(Provider provider)
    {
        var content = await unitOfWork.DroppedContentRepository.GetAll();

        return content.Select(c => new DroppedContentAdaptor(c));
    }

    public Task<MessageDto> RelayMessage(MessageDto message, CancellationToken ct = default)
    {
        throw new BadRequestException("Dropped content does not support messages");
    }
}
