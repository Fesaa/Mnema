using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Publication;

namespace Mnema.Providers;

internal sealed record IoWork(Stream Stream, string FilePath, string Url, int Idx);

internal sealed record DownloadWork(int Idx, DownloadUrl Url);

internal sealed record DownloadContext
{
    public ChannelReader<DownloadWork> Reader { get; init; }
    public ChannelWriter<IoWork> Writer { get; init; }
    public Chapter Chapter { get; init; }
}

internal partial class Publication
{

    private readonly IHttpClientFactory _httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    
    public Task DownloadContentAsync(CancellationTokenSource tokenSource)
    {
        if (State != ContentState.Waiting && State != ContentState.Ready)
        {
            _logger.LogWarning("Publication is not in a valid state ({State}) to start, ignoring request", State.ToString());
            return Task.CompletedTask;
        }

        State = ContentState.Downloading;
        
        _tokenSource = tokenSource;

        try
        {
            return Download();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurring download {Title}/{Id}", Title, Id);
            return Cancel();
        }
    }

    private async Task Download()
    {
        if (Series == null)
            throw new MnemaException("Publication is downloading before series has loaded");
        
        var hook = scope.ServiceProvider.GetKeyedService<IPreDownloadHook>(provider);
        if (hook != null)
        {
            await hook.PreDownloadHook(this, scope, _tokenSource.Token);
        }

        if (_userSelectedIds.Count > 0)
        {
            var initialSize = _queuedChapters.Count;

            _queuedChapters = Series.Chapters.Select(c => c.Id).Where(_userSelectedIds.Contains).ToList();
            
            _logger.LogDebug("Chapters filtered after user selection. Old: {Old}, New: {New}", initialSize, _queuedChapters.Count);

            if (ToRemovePaths.Count > 0)
            {
                var paths = _queuedChapters
                    .Select(id => Series.Chapters.FirstOrDefault(c => c.Id == id))
                    .WhereNotNull()
                    .Select(c => ChapterPath(c) + "cbz")
                    .ToList();

                ToRemovePaths = ToRemovePaths.Where(paths.Contains).ToList();
            }
            
        }
        
        
        _logger.LogInformation("Will be downloading {Chapters}, and removing {ToDelete} chapters from {Provider} into {Dir}",
            _queuedChapters.Count, ToRemovePaths.Count, provider.ToString(), DownloadDir);

        _speedTracker = new SpeedTracker(_queuedChapters.Count);

        var sw = Stopwatch.StartNew();

        _settings = await _settingsService.GetSettingsAsync();
        _limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = _settings.MaxConcurrentImages,
            Window = TimeSpan.FromSeconds(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10000,
        });
        var maxImages = _settings.MaxConcurrentImages;

        var ioChannel = Channel.CreateBounded<IoWork>(maxImages * 2);

        var workers = Enumerable.Range(0, maxImages * 2).Select(_ => IoWorker(ioChannel)).ToList();
        workers.Add(ProcessDownloads(ioChannel));

        await Task.WhenAll(workers);
        
        _logger.LogInformation("Downloaded all chapters in {Elapsed}ms", sw.ElapsedMilliseconds);

        State = ContentState.Cleanup;

        await _publicationManager.StopDownload(StopRequest(false));
    }

    private async Task IoWorker(Channel<IoWork> channel)
    {
        await foreach (var ioWork in channel.Reader.ReadAllAsync(_tokenSource.Token))
        {
            try
            {
                var filePath = await _extensions.DownloadCallback(this, ioWork, _tokenSource.Token);
                
                _logger.LogTrace("Wrote {FilePath} / {Idx} to disk", filePath, ioWork.Idx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occured while handling I/O");
                await Cancel();
            }
        }
    }

    private async Task ProcessDownloads(Channel<IoWork> channel)
    {
        var sw = Stopwatch.StartNew();
        
        foreach (var chapterId in _queuedChapters)
        {
            var chapter = Series!.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (chapter == null)
            {
                _logger.LogWarning("Not downloading chapter with id {Id}, no matching info found", chapterId);
                continue;
            }

            await DownloadChapter(channel, chapter);
        }
        
        channel.Writer.Complete();

        _logger.LogDebug("All content has been downloaded in {Elapsed}ms, waiting for I/O to complete", sw.ElapsedMilliseconds);
    }

    private async Task DownloadChapter(Channel<IoWork> channel, Chapter chapter)
    {

        var urls = await _repository.ChapterUrls(chapter, _tokenSource.Token);

        if (urls.Count == 0)
        {
            _logger.LogWarning("Chapter has no urls to download. Unexpected? Report this!");
            return;
        }

        var chapterPath = ChapterPath(chapter);
        _fileSystem.Directory.CreateDirectory(chapterPath);
        
        // Mark as downloaded as soon as the directory is created as we need to remove it in case of an error
        DownloadedPaths.Add(chapterPath);

        try
        {
            await WriteMetadataForChapter(chapter);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An exception occured while writing metadata");
        }
        
        _logger.LogDebug("Starting download of chapter {ChapterMarker} with {Count} urls", chapter.ChapterMarker, urls.Count);

        var sw = Stopwatch.StartNew();
        
        _speedTracker!.SetIntermediate(urls.Count);

        var urlChannel = BuildUrlChannel(urls);

        await Task.WhenAll(Enumerable.Range(0, _settings.MaxConcurrentImages)
            .Select(_ => DownloadWorker(new DownloadContext
            {
                Reader = urlChannel.Reader,
                Writer = channel.Writer,
                Chapter = chapter,
            })));
        
        _logger.LogDebug("Finished downloading chapter {Chapter} in {Elapsed}ms", chapter.ChapterMarker, sw.ElapsedMilliseconds);

        if (urls.Count < 5)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        
        _speedTracker!.ClearIntermediate();
        _speedTracker!.Increment();
    }

    private async Task DownloadWorker(DownloadContext ctx)
    {
        var failedTasks = await ProcessDownloadsAsync(ctx, isRetry: false);
        
        if (failedTasks.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Some tasks failed to complete, retrying. Count: {Count}", failedTasks.Count);

        var retryChannel = Channel.CreateUnbounded<DownloadWork>();
        foreach (var task in failedTasks)
        {
            retryChannel.Writer.TryWrite(task);
        }
        retryChannel.Writer.Complete();

        await ProcessDownloadsAsync(ctx, isRetry: true);
    }

    private async Task<List<DownloadWork>> ProcessDownloadsAsync(DownloadContext ctx, bool isRetry)
    {
        var failedTasks = new List<DownloadWork>();
        var client = _httpClientFactory.CreateClient(provider.ToString());

        await foreach (var task in ctx.Reader.ReadAllAsync(_tokenSource.Token))
        {
            if (_tokenSource.Token.IsCancellationRequested)
            {
                return failedTasks;
            }

            using var lease = await _limiter.AcquireAsync(cancellationToken: _tokenSource.Token);
            if (!lease.IsAcquired)
            {
                _logger.LogWarning("Failed to acquire rate limiter lease for {Url}", task.Url);
                continue;
            }

            var url = isRetry && !string.IsNullOrEmpty(task.Url.FallbackUrl) ? task.Url.FallbackUrl : task.Url.Url;

            _logger.LogTrace("Processing task {Idx} with URL {Url}", task.Idx, url);

            try
            {
                var stream = await client.GetStreamAsync(url);

                _speedTracker!.IncrementIntermediate();

                await ctx.Writer.WriteAsync(new IoWork(stream, ChapterPath(ctx.Chapter), url, task.Idx));
            }
            catch (Exception ex)
            {
                if (isRetry) throw;
                
                _logger.LogWarning(ex, "Task {Idx} on {Url} has failed failed for the first time, retrying later", task.Idx, url);
                failedTasks.Add(task);
            }
        }

        return failedTasks;
    }

    private Channel<DownloadWork> BuildUrlChannel(IEnumerable<DownloadUrl> urls)
    {
        var channel = Channel.CreateUnbounded<DownloadWork>();

        var idx = 0;
        foreach (var url in urls)
        {
            if (!channel.Writer.TryWrite(new DownloadWork(++idx, url)))
            {
                _logger.LogWarning("Failed to write {Url} to channel", url);
            }
        }

        channel.Writer.Complete();
        
        return channel;
    }
    
}