using System.Diagnostics;
using Hangfire;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Models.Internal;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mnema.Models.Entities.Content;

namespace Mnema.Metadata.Mangabaka;

internal sealed record MangabakaIndexerSeries(int Id, string Title, string? NativeTitle);

public class MangabakaScheduler(
    ILogger<MangabakaScheduler> logger,
    IRecurringJobManagerV2 recurringJobManager,
    HttpClient httpClient,
    ApplicationConfiguration configuration,
    IServiceScopeFactory scopeFactory
    ): IScheduled
{
    private const string JobId = "metadata.mangabaka";
    private const string CronExpression = "0 1 * * 0";
    private const string DatabaseUrl = "https://api.mangabaka.dev/v1/database/series.sqlite.tar.gz";
    public const string DatabaseName = "Mnema.Metadata.Mangabaka.db";
    public const string LuceneIndexName = "Mnema.Metadata.Langabaka.Lucene";

    public static readonly LuceneVersion Version = LuceneVersion.LUCENE_48;

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
        var sw = Stopwatch.StartNew();

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

        logger.LogDebug("Downloaded database {DbPath} in {Elapsed}s", dbPath, sw.Elapsed.TotalSeconds);

        await ReIndexLucene(ct);
    }

    private async Task ReIndexLucene(CancellationToken ct)
    {
        logger.LogInformation("Reindexing {Index}", LuceneIndexName);
        var sw = Stopwatch.StartNew();

        using var scope = scopeFactory.CreateScope();

        var searchManager = scope.ServiceProvider.GetRequiredKeyedService<SearcherManager>(MetadataProvider.Mangabaka);
        var indexPath = scope.ServiceProvider.GetRequiredKeyedService<FSDirectory>(MetadataProvider.Mangabaka);
        var ctx = scope.ServiceProvider.GetRequiredService<MangabakaDbContext>();

        var writerConfig = new IndexWriterConfig(Version, new StandardAnalyzer(Version));

        using var writer = new IndexWriter(indexPath, writerConfig);

        writer.DeleteAll();

        var query = ctx.Series
            .Select(s => new {s.Id, s.Title, s.NativeTitle})
            .OrderBy(s => s.Id);

        await foreach (var series in BatchedSeries(ctx, ct: ct))
        {
            var document = new Document();
            document.AddStringField(nameof(series.Id), series.Id.ToString(), Field.Store.YES);
            document.AddTextField(nameof(MangabakaSeries.Title), series.Title, Field.Store.NO);

            if (series.NativeTitle != null)
                document.AddTextField(nameof(MangabakaSeries.NativeTitle), series.NativeTitle, Field.Store.NO);

            writer.AddDocument(document);
        }

        writer.Commit();

        searchManager.MaybeRefresh();

        logger.LogDebug("Reindexing {Index} in {Elapsed}s", LuceneIndexName, sw.Elapsed.TotalSeconds);
    }

    private static async IAsyncEnumerable<MangabakaIndexerSeries> BatchedSeries(MangabakaDbContext ctx,
        int batchSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {

        var currentCursor = 0;
        var hasMore = true;

        while (hasMore)
        {
            var seekId = currentCursor;

            var items = await ctx.Series
                .AsNoTracking()
                .Select(s => new { s.Id , s.Title, s.NativeTitle})
        .OrderBy(s => s.Id)
                .Where(s => s.Id > seekId)
                .Take(batchSize)
                .ToListAsync(cancellationToken: ct);

            if (items.Count == 0)
                yield break;

            foreach (var series in items)
                yield return new MangabakaIndexerSeries(series.Id, series.Title, series.NativeTitle);

            currentCursor = items[^1].Id;
            hasMore = items.Count >= batchSize;
        }
    }
}
