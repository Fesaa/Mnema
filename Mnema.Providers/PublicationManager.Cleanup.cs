using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Providers;

internal partial class PublicationManager
{

    private async Task CleanupAfterDownload(IPublication publication, bool deleteFiles)
    {
        if (deleteFiles)
        {
            await DeleteFiles(publication);
            return;
        }

        await CleanUp(publication);

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (publication.Request.IsSubscription)
        {
            var sub = await unitOfWork.SubscriptionRepository.GetSubscription(publication.Request.SubscriptionId!.Value);
            if (sub != null)
            {
                sub.LastDownloadDir = Path.Join(BaseDir, publication.DownloadDir);
                await unitOfWork.CommitAsync();
            }

            if (publication.DownloadedPaths.Count > 0)
            {
                await AddNotification(new Notification
                {
                    Title = "Download completed",
                    UserId = publication.Request.UserId,
                    Summary = $"<a class=\"hover:pointer hover:underline\" href=\"%s\" target=\"_blank\">{publication.DownloadInfo.RefUrl}</a> finished downloading {publication.DownloadedPaths.Count} item(s)",
                    Colour = NotificationColour.Primary,
                });
            }
            
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

        directory = Path.Join(BaseDir, directory);

        if (!_fileSystem.Directory.Exists(directory))
        {
            _logger.LogDebug("Directory to cleanup does not exist: {Directory}", directory);
            return Task.CompletedTask;
        }

        foreach (var path in publication.DownloadedPaths)
        {
            _logger.LogTrace("Deleting newly downloaded directory: {Directory}", path);

            try
            {
                _fileSystem.Directory.Delete(path, true);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to delete newly downloaded directory {Directory}, you may have lingering content", path);
            }
        }
        
        foreach (var subDirectory in _fileSystem.Directory.EnumerateDirectories(directory))
        {
            if (_fileSystem.Directory.EnumerateFileSystemEntries(subDirectory).Any())
            {
                continue;
            }
                
            _logger.LogTrace("Found empty subdirectory {SubDirectory} removing", subDirectory);

            try
            {
                _fileSystem.Directory.Delete(subDirectory, false);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to delete subdirectory {SubDirectory}", subDirectory);
            }
        }
        
        _logger.LogDebug("Finished removing newly downloaded items  in {Directory} in {Elapsed}ms", directory, sw.ElapsedMilliseconds);

        return Task.CompletedTask;
    }

    private async Task CleanUp(IPublication publication)
    {
        if (publication.DownloadedPaths.Count == 0)
        {
            _logger.LogDebug("No newly downloaded items for {Id} - {Title}", publication.Id, publication.Title);
            return;
        }

        var sw = Stopwatch.StartNew();
        
        foreach (var path in publication.ToRemovePaths)
        {
            _logger.LogTrace("Removing old chapter on {Path}", path);
            
            try
            {
                _fileSystem.File.Delete(path);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to delete old file {File}", path);
            }
        }
        
        foreach (var path in publication.DownloadedPaths)
        {
            _logger.LogTrace("Finalizing chapter {Path}", path);

            try
            {
                await publication.FinalizeChapter(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occured finishing up a chapter at {Path}", path);
            }
        }
        
        _logger.LogDebug("Cleanup up {Id} - {Title} in {Elapsed}ms, removed {Deleted} old files, added {New} new files",
            publication.Id, publication.Title, sw.ElapsedMilliseconds, publication.ToRemovePaths.Count, publication.DownloadedPaths.Count);
    }
    
}