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
            if (!skipSaving)
            {
                await CleanUp(publication);

                if (publication.Request.IsSubscription)
                {
                    if (publication.DownloadedPaths.Count > 0)
                    {
                        await AddNotification(new Notification
                        {
                            Title = "Download completed",
                            UserId = publication.Request.UserId,
                            Summary =
                                $"<a class=\"hover:pointer hover:underline\" href=\"%s\" target=\"_blank\">{publication.DownloadInfo.RefUrl}</a> finished downloading {publication.DownloadedPaths.Count} item(s)",
                            Colour = NotificationColour.Primary,
                        });
                    }
                }
            }

            await DeleteFiles(publication);
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
                _fileSystem.File.Delete(_fileSystem.Path.Join(_configuration.BaseDir, path));
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to delete old file {File}", path);
            }
        }

        var baseDir = _fileSystem.Path.Join(_configuration.BaseDir, publication.DownloadDir);
        if (!_fileSystem.Directory.Exists(baseDir))
        {
            _logger.LogDebug("Base directory {Dir} does not exist, creating", baseDir);
            _fileSystem.Directory.CreateDirectory(baseDir);
        }
        
        foreach (var path in publication.DownloadedPaths)
        {
            _logger.LogTrace("Finalizing chapter {Path}", path);

            try
            {
                var src = _fileSystem.Path.Join(_configuration.DownloadDir, path);
                var dest = _fileSystem.Path.Join(_configuration.BaseDir, path);
                
                await publication.FinalizeChapter(src, dest);
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