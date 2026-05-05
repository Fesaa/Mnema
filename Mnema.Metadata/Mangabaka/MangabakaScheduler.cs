using System.Diagnostics;
using Hangfire;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Models.Internal;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Cjk;
using Lucene.Net.Analysis.Ja;
using Lucene.Net.Analysis.Miscellaneous;
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
using Directory = System.IO.Directory;

namespace Mnema.Metadata.Mangabaka;

public static class MangabakaFields
{
    public const string Id = "id";
    public const string Title = "title";
    public const string JaTitle = "ja_title";
    public const string KoTitle = "ko_title";
    public const string ZhTitle = "zh_title";

    public static readonly string[] TitleFields = [Title, JaTitle, KoTitle, ZhTitle];

    public static Analyzer PerFieldAnalyzer()
    {
        var analyzerMap = new Dictionary<string, Analyzer>
        {
            [Title] = new StandardAnalyzer(MangabakaScheduler.Version),
            [JaTitle] = new JapaneseAnalyzer(MangabakaScheduler.Version),
            [KoTitle] = new CJKAnalyzer(MangabakaScheduler.Version),
            [ZhTitle] = new CJKAnalyzer(MangabakaScheduler.Version)
        };

        return new PerFieldAnalyzerWrapper(new StandardAnalyzer(MangabakaScheduler.Version), analyzerMap);
    }
}

internal sealed record MangabakaIndexerSeries(int Id, List<MangabakaTitle> Titles);

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
    public const string LuceneIndexName = "Mnema.Metadata.Mangabaka.Lucene";

    public const LuceneVersion Version = LuceneVersion.LUCENE_48;

    private static readonly RecurringJobOptions RecurringJobOptions = new() { TimeZone = TimeZoneInfo.Local };

    public async Task EnsureScheduledAsync(CancellationToken cancellationToken)
    {
        recurringJobManager.AddOrUpdate<MangabakaScheduler>(JobId,
            s => s.DownloadDatabase(CancellationToken.None),
            CronExpression, RecurringJobOptions);

        var dbPath = Path.Join(configuration.PersistentStorage, DatabaseName);
        var indexPath = Path.Join(configuration.PersistentStorage, LuceneIndexName);

        if (!File.Exists(dbPath))
        {
            BackgroundJob.Enqueue(() => DownloadDatabase(CancellationToken.None));
        }
        else if (!File.Exists(indexPath))
        {
            await ReIndexLucene(cancellationToken);
        }

        // Correct typo from the past
        if (Directory.Exists("Mnema.Metadata.Langabaka.Lucene"))
        {
            Directory.Delete("Mnema.Metadata.Langabaka.Lucene", true);
        }
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

        var writerConfig = new IndexWriterConfig(Version, MangabakaFields.PerFieldAnalyzer());
        using var writer = new IndexWriter(indexPath, writerConfig);

        writer.DeleteAll();

        await foreach (var series in BatchedSeries(ctx, ct: ct)
                           .Where(s => s.Titles.Count > 0)
                           .WithCancellation(ct))
        {
            var document = new Document
            {
                new StringField(MangabakaFields.Id, series.Id.ToString(), Field.Store.YES)
            };

            foreach (var title in series.Titles)
            {
                var field = title.Language switch
                {
                    "ja" => MangabakaFields.JaTitle,
                    "ko" => MangabakaFields.KoTitle,
                    "zh" or "zh-hk" => MangabakaFields.ZhTitle,
                    _ => MangabakaFields.Title
                };

                document.AddTextField(field, title.Title, Field.Store.YES);
            }

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
                .Where(s => s.MergedWith == null)
                .Select(s => new { s.Id , s.Titles})
                .OrderBy(s => s.Id)
                .Where(s => s.Id > seekId)
                .Take(batchSize)
                .ToListAsync(cancellationToken: ct);

            if (items.Count == 0)
                yield break;

            foreach (var series in items)
                yield return new MangabakaIndexerSeries(
                    series.Id,
                    series.Titles ?? []
                    );

            currentCursor = items[^1].Id;
            hasMore = items.Count >= batchSize;
        }
    }
}
