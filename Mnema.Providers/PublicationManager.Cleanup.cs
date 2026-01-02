using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Providers;

internal partial class PublicationManager
{

    private async Task CleanupAfterDownload(IPublication publication, bool skipSaving)
    {
        using var scope = _scopeFactory.CreateScope();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
        
        try
        {
            if (publication.State != ContentState.Cancel)
            {
                if (!skipSaving)
                {
                    await publication.Cleanup();
                }

                await DeleteFiles(publication);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occured while cleaning up download for {Id} - {Title}", publication.Id,
                publication.Title);
        }
        finally
        {
            await messageService.DeleteContent(publication.Request.UserId, publication.Id);
        }
    }

    private Task DeleteFiles(IPublication publication)
    {
        var directory = publication.DownloadDir.Trim();
        if (string.IsNullOrWhiteSpace(directory))
        {
            _logger.LogError("Download directory of {Id} - {Title} was empty, not deleting any files", publication.Id, publication.Title);
            return Task.CompletedTask;
        }

        var sw = Stopwatch.StartNew();

        directory = Path.Join(_configuration.DownloadDir, directory);
        if (string.IsNullOrEmpty(directory) || !_fileSystem.Directory.Exists(directory))
        {
            _logger.LogDebug("Directory to cleanup does not exist: {Directory}", directory);
            return Task.CompletedTask;
        }

        _fileSystem.Directory.Delete(directory, true);

        _logger.LogDebug("Finished removing files in {Directory} in {Elapsed}ms", directory, sw.ElapsedMilliseconds);

        return Task.CompletedTask;
    }
    
}