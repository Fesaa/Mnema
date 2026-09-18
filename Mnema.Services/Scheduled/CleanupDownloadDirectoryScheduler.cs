using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Models.Internal;

namespace Mnema.Services.Scheduled;

public class CleanupDownloadDirectoryScheduler(
    ILogger<CleanupDownloadDirectoryScheduler> logger,
    IRecurringJobManagerV2 recurringJobManager,
    IFileSystem fileSystem,
    ApplicationConfiguration configuration) : IScheduled
{

    private const string CronJob = "0 3 * * *";
    private const string JobId = "download-directory-cleanup";

    public Task EnsureScheduledAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Scheduling download directory cleanup job");

        recurringJobManager.AddOrUpdate<CleanupDownloadDirectoryScheduler>(JobId,
            s => s.CleanupDownloadDirectory(), CronJob,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

        return Task.CompletedTask;
    }

    public async Task CleanupDownloadDirectory()
    {
        var downloadDir = configuration.DownloadDir;

        if (!fileSystem.Directory.Exists(downloadDir))
        {
            logger.LogWarning("Download directory {Directory} does not exist. Skipping cleanup.", downloadDir);
            return;
        }

        logger.LogInformation("Starting cleanup of empty directories in {Directory}", downloadDir);

        // Order paths by descending length (deepest subdirectories first)
        var directories = fileSystem.Directory
            .EnumerateDirectories(downloadDir, "*", SearchOption.AllDirectories)
            .OrderByDescending(path => path.Length);

        int deletedCount = 0;

        foreach (var directory in directories)
        {
            try
            {
                var isEmpty = !fileSystem.Directory.EnumerateFileSystemEntries(directory).Any();

                if (isEmpty)
                {
                    fileSystem.Directory.Delete(directory, false);
                    deletedCount++;

                    logger.LogTrace("Deleted empty directory: {DirectoryPath}", directory);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete directory: {DirectoryPath}", directory);
            }
        }

        logger.LogInformation("Finished cleanup. Removed {Count} empty directories.", deletedCount);

        await Task.CompletedTask;
    }
}
