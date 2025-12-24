using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Services;

internal class DownloadService(ILogger<DownloadService> logger, IServiceScopeFactory scopeFactory): IDownloadService
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

    public async Task<IList<DownloadInfo>> GetCurrentContent(Guid userId)
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
            
            downloads.AddRange(content
                .Where(c => c.Request.UserId == userId)
                .Select(c => c.DownloadInfo));
        }

        return downloads;
    }
}