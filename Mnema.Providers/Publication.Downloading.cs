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
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.User;
using Mnema.Models.Publication;

namespace Mnema.Providers;

internal sealed record IoWork(UserPreferences Preferences, Stream Stream, string FilePath, string Url, int Idx);

internal sealed record DownloadWork(int Idx, DownloadUrl Url);

internal sealed record DownloadContext
{
    public ChannelReader<DownloadWork> Reader { get; init; }
    public ChannelWriter<IoWork> Writer { get; init; }
    public Chapter Chapter { get; init; }
}

internal partial class Publication
{
    private readonly IHttpClientFactory _httpClientFactory =
        scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    private readonly IImageService _imageService = scope.ServiceProvider.GetRequiredService<IImageService>();

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

            QueuedChapters = Series.Chapters.Select(c => c.Id).Where(_userSelectedIds.Contains).ToList();

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
        var maxImages = _settings.MaxConcurrentImages;

        var ioChannel = Channel.CreateBounded<IoWork>(maxImages * 2);

        var workers = Enumerable.Range(0, maxImages * 2).Select(_ => IoWorker(ioChannel)).ToList();
        workers.Add(ProcessDownloads(ioChannel));

        _ = Task.Run(SignalRUpdateLoop, _tokenSource.Token);

        _ioTask = Task.WhenAll(workers);

        await _ioTask;

        _logger.LogInformation("[{Title}/{Id}] Downloaded all chapters in {Elapsed}ms",
            Title, Id, sw.ElapsedMilliseconds);

        State = ContentState.Cleanup;
        await _messageService.StateUpdate(Request.UserId, Id, ContentState.Cleanup);

        await _publicationManager.StopDownload(StopRequest(false));
    }

    private async Task IoWorker(Channel<IoWork> channel)
    {
        await foreach (var ioWork in channel.Reader.ReadAllAsync(_tokenSource.Token))
            try
            {
                await using (ioWork.Stream)
                {
                    if (_tokenSource.IsCancellationRequested || !Path.Exists(ioWork.FilePath)) continue;

                    var realFileType = ioWork.Url.GetFileType();
                    var fileType = ioWork.Preferences.ImageFormat.GetFileExtension(ioWork.Url);

                    var fileCounter = $"{ioWork.Idx}".PadLeft(4, '0');
                    var filePath = Path.Join(ioWork.FilePath, $"page {fileCounter}{fileType}");

                    var format = ioWork.Preferences.ImageFormat;
                    if (ioWork.Preferences.ImageFormat == ImageFormat.Webp && realFileType != ".webp")
                    {
                        format = ImageFormat.Upstream;
                    }

                    await _imageService.ConvertAndSave(ioWork.Stream, format, filePath, _tokenSource.Token);

                    _logger.LogTrace("[{Title}/{Id}] Wrote {FilePath} / {Idx} to disk", Title, Id, filePath, ioWork.Idx);
                }
            }
            catch (TaskCanceledException)
            {
                /* Ignored */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Title}/{Id}] An exception occured while handling I/O", Title, Id);
                await Cancel(ex);
            }
    }

    private async Task ProcessDownloads(Channel<IoWork> channel)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            foreach (var chapterId in QueuedChapters)
            {
                if (_tokenSource.Token.IsCancellationRequested) break;

                var chapter = Series!.Chapters.FirstOrDefault(c => c.Id == chapterId);
                if (chapter == null)
                {
                    _logger.LogWarning("[{Title}/{Id}] Not downloading chapter with id {ChapterId}, no matching info found", Title, Id, chapterId);
                    continue;
                }

                await DownloadChapter(channel, chapter);
                await _messageService.UpdateContent(Request.UserId, DownloadInfo);
            }

            _logger.LogDebug("[{Title}/{Id}] All content has been downloaded in {Elapsed}ms, waiting for I/O to complete",
                Title, Id, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            channel.Writer.TryComplete(ex);
        }
        finally
        {
            channel.Writer.TryComplete();
        }
    }

    private async Task DownloadChapter(Channel<IoWork> channel, Chapter chapter)
    {
        var urls = await _repository.ChapterUrls(chapter, _tokenSource.Token);

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

        await Task.WhenAll(Enumerable.Range(0, _settings.MaxConcurrentImages)
            .Select(_ => DownloadWorker(new DownloadContext
            {
                Reader = urlChannel.Reader,
                Writer = channel.Writer,
                Chapter = chapter
            })));

        _logger.LogTrace("[{Title}/{Id}] Finished downloading chapter {Chapter} in {Elapsed}ms",
            Title, Id, chapter.ChapterMarker, sw.ElapsedMilliseconds);

        if (urls.Count < 5) await Task.Delay(TimeSpan.FromSeconds(1));

        _speedTracker!.ClearIntermediate();
        _speedTracker!.Increment();
    }

    private async Task DownloadWorker(DownloadContext ctx)
    {
        var failedTasks = await ProcessDownloadsAsync(ctx, false);

        if (failedTasks.Count == 0 || _tokenSource.Token.IsCancellationRequested) return;

        _logger.LogDebug("[{Title}/{Id}] Some tasks failed to complete, retrying. Count: {Count}", Title, Id, failedTasks.Count);
        _failedDownloadsTracker += failedTasks.Count;

        var retryChannel = Channel.CreateUnbounded<DownloadWork>();
        foreach (var task in failedTasks) retryChannel.Writer.TryWrite(task);
        retryChannel.Writer.Complete();

        await ProcessDownloadsAsync(ctx, true);
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
                var stream = await client.GetStreamAsync(url);

                _speedTracker!.IncrementIntermediate();

                await ctx.Writer.WriteAsync(new IoWork(Preferences, stream, ChapterPath(ctx.Chapter), url, task.Idx));
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
