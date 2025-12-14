using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;

namespace Mnema.Providers;

public sealed class PublicationManager : IPublicationManager, IAsyncDisposable
{
    private readonly ILogger<PublicationManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, IPublication> _content = new();
    
    private readonly Channel<IPublication> _loadingChannel;
    private readonly Channel<IPublication> _downloadingChannel;
    
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _workerTask;
    
    public string BaseDir { get; private set; } = null!;

    public PublicationManager(ILogger<PublicationManager> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        var channelOptions = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        
        _loadingChannel = Channel.CreateBounded<IPublication>(channelOptions);
        _downloadingChannel = Channel.CreateBounded<IPublication>(channelOptions);

        _workerTask = Task.Run(WorkerAsync);
        
        _logger.LogDebug("PublicationManager initialized");
    }

    public async Task Download(DownloadRequestDto request)
    {
        if (_content.ContainsKey(request.Id))
        {
            throw new MnemaException("Content already exists");
        }

        var publication = CreatePublication(request);
        
        if (!_content.TryAdd(publication.Id, publication))
        {
            throw new MnemaException("Failed to add content");
        }

        try
        {
            await AddToLoadingQueueAsync(publication);
        }
        catch
        {
            _content.TryRemove(publication.Id, out _);
            throw;
        }
    }

    public Task StopDownload(StopRequestDto request)
    {
        if (!_content.TryRemove(request.Id, out var publication))
        {
            throw new MnemaException("Content not found");
        }

        _logger.LogInformation("Removing content: {Id} - {Title}, DeleteFiles: {DeleteFiles}",
            request.Id, publication.Title, request.DeleteFiles);

        publication.Cancel();
        
        return Task.CompletedTask;
    }

    public async Task MoveToDownloadQueue(string id)
    {
        if (!_content.TryGetValue(id, out var publication))
        {
            throw new MnemaException("Content not found");
        }

        await AddToDownloadQueueAsync(publication);
    }

    public Task<IEnumerable<IContent>> GetAllContent()
    {
        return Task.FromResult<IEnumerable<IContent>>(_content.Values.ToList());
    }

    public Task<IPublication> GetPublicationById(string id)
    {
        if (!_content.TryGetValue(id, out var publication))
        {
            throw new MnemaException("Content not found");
        }

        return Task.FromResult(publication);
    }

    private async Task AddToLoadingQueueAsync(IPublication publication)
    {
        try
        {
            await _loadingChannel.Writer.WriteAsync(publication, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException("Queue is shutting down");
        }
        catch (ChannelClosedException)
        {
            throw new InvalidOperationException("Queue is closed");
        }
    }

    private async Task AddToDownloadQueueAsync(IPublication publication)
    {
        try
        {
            await _downloadingChannel.Writer.WriteAsync(publication, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException("Queue is shutting down");
        }
        catch (ChannelClosedException)
        {
            throw new InvalidOperationException("Queue is closed");
        }
    }

    /// <summary>
    /// Worker that prioritizes loading queue over download queue
    /// Matches the Go implementation's select statement behavior
    /// </summary>
    private async Task WorkerAsync()
    {
        _logger.LogDebug("Worker started");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (_loadingChannel.Reader.TryRead(out var loadingContent))
                {
                    await ProcessLoadInfoAsync(loadingContent);
                    continue;
                }

                var loadingTask = _loadingChannel.Reader.ReadAsync(_cts.Token).AsTask();
                var downloadingTask = _downloadingChannel.Reader.ReadAsync(_cts.Token).AsTask();

                var completedTask = await Task.WhenAny(loadingTask, downloadingTask);

                if (completedTask == loadingTask)
                {
                    var content = await loadingTask;
                    await ProcessLoadInfoAsync(content);
                }
                else
                {
                    var content = await downloadingTask;
                    await content.DownloadContentAsync(_cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Worker cancelled");
        }
        catch (ChannelClosedException)
        {
            _logger.LogDebug("Channel closed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker encountered an error");
        }
        finally
        {
            _logger.LogDebug("Worker stopped");
        }
    }

    /// <summary>
    /// Processes loading metadata for a publication
    /// If ready, moves to download queue automatically
    /// </summary>
    private async Task ProcessLoadInfoAsync(IPublication publication)
    {
        _logger.LogDebug("Starting load info for {Id} - {Title}", publication.Id, publication.Title);

        await publication.LoadMetadataAsync(_cts.Token);

        if (publication.State == ContentState.Ready)
        {
            _logger.LogTrace("Content {Id} is ready after loading, moving to download queue", publication.Id);
            await AddToDownloadQueueAsync(publication);
        }
    }

    private Publication CreatePublication(DownloadRequestDto request)
    {
        var scope = _scopeFactory.CreateScope();
        
        var publication = new Publication(scope, request.Provider, request);

        return publication;
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Shutting down PublicationManager");

        await _cts.CancelAsync();

        _loadingChannel.Writer.Complete();
        _downloadingChannel.Writer.Complete();

        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
        var completedTask = await Task.WhenAny(_workerTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _logger.LogWarning("PublicationManager shutdown timeout");
        }
        else
        {
            _logger.LogDebug("PublicationManager shutdown complete");
        }

        _cts.Dispose();
    }
}