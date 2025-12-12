using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.DTOs.Content;

namespace Mnema.Services;

public class DownloadService(ILogger<DownloadService> logger, IServiceScopeFactory scopeFactory): IDownloadService
{

    public Task StartDownload(DownloadRequestDto request)
    {
        using var scope = scopeFactory.CreateScope();
        var contentManager = scope.ServiceProvider.GetRequiredKeyedService<IContentManager>(request.Provider.ToString());

        return contentManager.Download(request);
    }

    public Task CancelDownload(StopRequestDto request)
    {
        throw new NotImplementedException();
    }

    public Task<IList<DownloadInfo>> GetCurrentContent()
    {
        throw new NotImplementedException();
    }
}