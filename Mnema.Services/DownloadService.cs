using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Services;

public class DownloadService(ILogger<DownloadService> logger, IServiceScopeFactory scopeFactory): IDownloadService
{

    public Task StartDownload(DownloadRequestDto request)
    {
        using var scope = scopeFactory.CreateScope();
        var contentManager = scope.ServiceProvider.GetRequiredKeyedService<IContentManager>(request.Provider);

        return contentManager.Download(request);
    }

    public Task CancelDownload(StopRequestDto request)
    {
        using var scope = scopeFactory.CreateScope();
        var contentManager = scope.ServiceProvider.GetRequiredKeyedService<IContentManager>(request.Provider);

        return contentManager.StopDownload(request);
    }

    public async Task<IList<DownloadInfo>> GetCurrentContent()
    {
        var downloads = new List<DownloadInfo>();
        
        using var scope = scopeFactory.CreateScope();
        foreach (var provider in Enum.GetValues<Provider>())
        {
            var contentManager = scope.ServiceProvider.GetKeyedService<IContentManager>(provider);
            if (contentManager == null)
            {
                logger.LogWarning("{Provider} has had no content manager registered, skipping", provider.ToString());
                continue;
            }

            var content = await contentManager.GetAllContent();
            
            downloads.AddRange(content.Select(c => new DownloadInfo
            {
                Provider = provider,
                Id = c.Id,
                ContentState = c.State,
                Name = c.Title,
                RefUrl = "",
                Size = "",
                Downloading = false,
                Progress = 0,
                Estimated = 0,
                SpeedType = SpeedType.Bytes,
                Speed = 0,
                DownloadDir = "",
            }));
        }

        return downloads;
    }
}