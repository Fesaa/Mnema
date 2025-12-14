using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Publication;

namespace Mnema.Providers;

internal sealed record IoWork();

internal partial class Publication
{
    
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
        if (_series == null)
            throw new MnemaException("Publication is downloading before series has loaded");
        
        var hook = scope.ServiceProvider.GetKeyedService<IPreDownloadHook>(provider);
        if (hook != null)
        {
            await hook.PreDownloadHook(this);
        }

        if (_userSelectedIds.Count > 0)
        {
            var initialSize = _queuedChapters.Count;

            _queuedChapters = _series.Chapters.Select(c => c.Id).Where(_userSelectedIds.Contains).ToList();
            
            _logger.LogDebug("Chapters filtered after user selection. Old: {Old}, New: {New}", initialSize, _queuedChapters.Count);

            if (ToRemovePaths.Count > 0)
            {
                var paths = _queuedChapters
                    .Select(id => _series.Chapters.FirstOrDefault(c => c.Id == id))
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

        var maxImages = await _settingsService.GetSettingsAsync<int>(ServerSettingKey.MaxConcurrentImages);

        var ioChannel = Channel.CreateBounded<IoWork>(maxImages * 2);

        var workers = Enumerable.Range(0, maxImages * 2).Select(_ => IoWorker(ioChannel)).ToList();
        workers.Add(ProcessDownloads(ioChannel));

        await Task.WhenAll(workers);
    }

    private async Task IoWorker(Channel<IoWork> channel)
    {
        await foreach (var ioWork in channel.Reader.ReadAllAsync(_tokenSource.Token))
        {
            try
            {
                await _extensions.DownloadCallback(this, ioWork);
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
            var chapter = _series!.Chapters.FirstOrDefault(c => c.Id == chapterId);
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
        
    }
    
}