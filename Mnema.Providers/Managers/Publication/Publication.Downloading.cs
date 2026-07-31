using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;
using Mnema.Models.Publication;

namespace Mnema.Providers.Managers.Publication;

internal sealed record IoWork(
    UserPreferences Preferences,
    Stream Stream,
    string FilePath,
    string Url,
    int Idx,
    string Format,
    SemaphoreSlim? ChapterBarrier);

internal sealed record DownloadWork(int Idx, DownloadUrl Url);

internal sealed record DownloadContext
{
    public ChannelReader<DownloadWork> Reader { get; set; }
    public Chapter Chapter { get; init; }
    public SemaphoreSlim? ChapterBarrier { get; init; }
}

internal partial class Publication
{
    private readonly IHttpClientFactory _httpClientFactory =
        scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    private readonly IIoHandler _ioHandler = scope.ServiceProvider.GetRequiredKeyedService<IIoHandler>(provider);

    private Task? _ioTask;

    public Task DownloadContentAsync(CancellationTokenSource tokenSource)
    {
        if (State != ContentState.Waiting && State != ContentState.Ready)
        {
            _logger.LogWarning("[{Title}/{Id}] Publication is not in a valid state ({State}) to start, ignoring request",
                Title, Id, State.ToString());
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
            _logger.LogError(ex, "[{Title}/{Id}] An exception occurring download", Title, Id);
            return Cancel(ex);
        }
    }

    private async Task Download()
    {
        if (Series == null)
            throw new MnemaException("Publication is downloading before series has loaded");

        await _messageService.StateUpdate(Request.UserId, Id, ContentState.Downloading);

        var hook = scope.ServiceProvider.GetKeyedService<IPreDownloadHook>(provider);
        if (hook != null) await hook.PreDownloadHook(this, scope, _tokenSource.Token);

        if (_userSelectedIds.Count > 0)
        {
            var initialSize = QueuedChapters.Count;

            QueuedChapters = Series.Chapters.Select(c => c.Id).Where(_userSelectedIds.Contains).ToHashSet();

            _logger.LogDebug("[{Title}/{Id}] Chapters filtered after user selection. Old: {Old}, New: {New}", Title, Id, initialSize,
                QueuedChapters.Count);

            if (ToRemovePaths.Count > 0)
            {
                var paths = QueuedChapters
                    .Select(id => Series.Chapters.FirstOrDefault(c => c.Id == id))
                    .WhereNotNull()
                    .Select(c => ChapterPath(c) + "cbz")
                    .ToList();

                ToRemovePaths = ToRemovePaths.Where(paths.Contains).ToList();
            }
        }


        _logger.LogInformation(
            "[{Title}/{Id}] Will be downloading {Chapters}, and removing {ToDelete} chapters from {Provider} into {Dir}",
            Title, Id, QueuedChapters.Count, ToRemovePaths.Count, provider.ToString(), DownloadDir);

        _speedTracker = new SpeedTracker(QueuedChapters.Count);

        _connectionService.CommunicateDownloadStarted(DownloadInfo);

        var sw = Stopwatch.StartNew();

        _settings = await _settingsService.GetSettingsAsync();
        _limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = _settings.MaxConcurrentImages,
            Window = TimeSpan.FromSeconds(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10000
        });

        _ = Task.Run(SignalRUpdateLoop, _tokenSource.Token);

        await ProcessDownloads();

        _logger.LogInformation("[{Title}/{Id}] Downloaded all chapters in {Elapsed}ms",
            Title, Id, sw.ElapsedMilliseconds);

        State = ContentState.Cleanup;
        await _messageService.StateUpdate(Request.UserId, Id, ContentState.Cleanup);

        await _publicationManager.StopDownload(StopRequest(false));
    }

    private async Task ProcessDownloads()
    {
        var sw = Stopwatch.StartNew();

        foreach (var chapterId in QueuedChapters)
        {
            if (_tokenSource.Token.IsCancellationRequested) break;

            var chapter = Series!.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (chapter == null)
            {
                _logger.LogWarning("[{Title}/{Id}] Not downloading chapter with id {ChapterId}, no matching info found", Title, Id, chapterId);
                continue;
            }

            await DownloadChapter(chapter);
            await _messageService.UpdateContent(Request.UserId, DownloadInfo);
        }

        _logger.LogDebug("[{Title}/{Id}] All content has been downloaded in {Elapsed}ms, waiting for I/O to complete",
            Title, Id, sw.ElapsedMilliseconds);
    }

    private async Task DownloadChapter(Chapter chapter)
    {
        var urls = await _repository.ChapterUrls(Request.Metadata, chapter, _tokenSource.Token);

        if (urls.Count == 0)
        {
            _logger.LogWarning("[{Title}/{Id}] Chapter has no urls to download. Unexpected? Report this!", Title, Id);
            return;
        }

        var chapterPath = ChapterPath(chapter);
        _fileSystem.Directory.CreateDirectory(chapterPath);

        // Mark as downloaded as soon as the directory is created as we need to remove it in case of an error
        DownloadedPaths.Add(chapterPath.RemovePrefix(_configuration.DownloadDir));

        try
        {
            await WriteMetadataForChapter(chapter);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{Title}/{Id}] An exception occured while writing metadata", Title, Id);
        }

        _logger.LogTrace("[{Title}/{Id}] Starting download of chapter {ChapterMarker} with {Count} urls",
            Title, Id, chapter.ChapterMarker, urls.Count);

        var sw = Stopwatch.StartNew();

        _speedTracker!.SetIntermediate(urls.Count);

        var urlChannel = BuildUrlChannel(urls);

        var pendingIo = new SemaphoreSlim(0);
        var expectedCount = urls.Count;

        await Task.WhenAll(Enumerable.Range(0, _settings.MaxConcurrentImages)
            .Select(_ => DownloadWorker(new DownloadContext
            {
                Reader = urlChannel.Reader,
                Chapter = chapter,
                ChapterBarrier = pendingIo
            })));

        if (provider.IsDirectDownload())
        {
            for (var i = 0; i < expectedCount; i++)
                await pendingIo.WaitAsync(_tokenSource.Token);
        }

        _logger.LogTrace("[{Title}/{Id}] Finished downloading chapter {Chapter} in {Elapsed}ms",
            Title, Id, chapter.ChapterMarker, sw.ElapsedMilliseconds);

        if (urls.Count < 5) await Task.Delay(TimeSpan.FromSeconds(1));

        _speedTracker!.ClearIntermediate();
        _speedTracker!.Increment();
    }

    private async Task DownloadWorker(DownloadContext ctx)
    {
        try
        {
            var failedTasks = await ProcessDownloadsAsync(ctx, false);

            if (failedTasks.Count == 0 || _tokenSource.Token.IsCancellationRequested) return;

            _logger.LogDebug("[{Title}/{Id}] Some tasks failed to complete, retrying. Count: {Count}", Title, Id,
                failedTasks.Count);
            _failedDownloadsTracker += failedTasks.Count;

            var retryChannel = Channel.CreateUnbounded<DownloadWork>();
            foreach (var task in failedTasks) retryChannel.Writer.TryWrite(task);
            retryChannel.Writer.Complete();

            ctx.Reader = retryChannel.Reader;
            await ProcessDownloadsAsync(ctx, true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (!_tokenSource.IsCancellationRequested)
                await Cancel(ex);
        }
    }

    private async Task<List<DownloadWork>> ProcessDownloadsAsync(DownloadContext ctx, bool isRetry)
    {
        var failedTasks = new List<DownloadWork>();
        var client = _httpClientFactory.CreateClient(provider.ToString());

        await foreach (var task in ctx.Reader.ReadAllAsync(_tokenSource.Token))
        {
            if (_tokenSource.Token.IsCancellationRequested) return failedTasks;

            using var lease = await _limiter.AcquireAsync(cancellationToken: _tokenSource.Token);
            if (!lease.IsAcquired)
            {
                _logger.LogWarning("[{Title}/{Id}] Failed to acquire rate limiter lease for {Url}", Title, Id, task.Url);
                continue;
            }

            var url = isRetry && !string.IsNullOrEmpty(task.Url.FallbackUrl) ? task.Url.FallbackUrl : task.Url.Url;

            _logger.LogTrace("[{Title}/{Id}] Processing task {Idx} with URL {Url}", Title, Id, task.Idx, url);

            try
            {
                await using var stream = await client.GetStreamAsync(url);
                var work = new IoWork(
                    Preferences,
                    stream,
                    ChapterPath(ctx.Chapter),
                    url,
                    task.Idx,
                    task.Url.Format,
                    ctx.ChapterBarrier);

                await _ioHandler.HandleIoWork(Title, Id, work, _tokenSource);

                _speedTracker!.IncrementIntermediate();
            }
            catch (Exception ex)
            {
                if (isRetry) throw;

                _logger.LogWarning(ex, "[{Title}/{Id}] Task {Idx} on {Url} has failed failed for the first time, retrying later",
                    Title, Id, task.Idx, url);
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
            if (!channel.Writer.TryWrite(new DownloadWork(++idx, url)))
                _logger.LogWarning("[{Title}/{Id}] Failed to write {Url} to channel", Title, Id, url);

        channel.Writer.Complete();

        return channel;
    }

    private async Task SignalRUpdateLoop()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

        try
        {
            while (await timer.WaitForNextTickAsync(_tokenSource.Token))
                await _messageService.UpdateContent(Request.UserId, DownloadInfo);
        }
        catch (OperationCanceledException)
        {
            /* Ignored */
        }
    }
}
