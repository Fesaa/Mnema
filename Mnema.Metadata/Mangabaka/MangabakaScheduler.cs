using Hangfire;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Models.Internal;
using System.Formats.Tar;
using System.IO.Compression;

namespace Mnema.Metadata.Mangabaka;

public class MangabakaScheduler(
    ILogger<MangabakaScheduler> logger,
    IRecurringJobManagerV2 recurringJobManager,
    HttpClient httpClient,
    ApplicationConfiguration configuration
    ): IScheduled
{
    private const string JobId = "metadata.mangabaka";
    private const string CronExpression = "0 1 * * *";
    private const string DatabaseUrl = "https://api.mangabaka.dev/v1/database/series.sqlite.tar.gz";
    public const string DatabaseName = "metadata.mangabaka.db";

    private static readonly RecurringJobOptions RecurringJobOptions = new()
    {
        TimeZone = TimeZoneInfo.Local
    };

    public async Task EnsureScheduledAsync()
    {
        recurringJobManager.AddOrUpdate<MangabakaScheduler>(JobId,
            s => s.DownloadDatabase(CancellationToken.None),
            CronExpression, RecurringJobOptions);

        var dbPath = Path.Join(configuration.PersistentStorage, DatabaseName);
        if (File.Exists(dbPath))
            return;

        await DownloadDatabase(CancellationToken.None);
    }

    public async Task DownloadDatabase(CancellationToken ct)
    {
        var dbPath = Path.Join(configuration.PersistentStorage, DatabaseName);
        var tempPath = Path.Join(configuration.PersistentStorage, $"{DatabaseName}.tar.gz");

        logger.LogInformation("Downloading database {RemoteUrl} into {DbPath}", DatabaseUrl, dbPath);

        try
        {
            var stream = await httpClient.GetStreamAsync(DatabaseUrl, ct);

            await using (var fileStream = File.Create(tempPath))
            {
                await stream.CopyToAsync(fileStream, ct);
            }

            await using (var compressedStream = File.OpenRead(tempPath))
            await using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            await using (var tarReader = new TarReader(gzipStream))
            {
                while (await tarReader.GetNextEntryAsync(false, ct) is { } entry)
                {
                    if (!entry.Name.EndsWith(".db", StringComparison.OrdinalIgnoreCase) &&
                        !entry.Name.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase)) continue;

                    await entry.ExtractToFileAsync(dbPath, overwrite: true, ct);
                    logger.LogDebug("Database extracted successfully to {DbPath}", dbPath);
                    break;
                }
            }
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
