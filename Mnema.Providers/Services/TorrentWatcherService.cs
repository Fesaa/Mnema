using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mnema.Providers.QBit;

namespace Mnema.Providers.Services;

internal class TorrentWatcherService(ILogger<TorrentWatcherService> logger, QBitContentManager qBitContentManager)
    : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(
            async _ => await DoWorkAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10)
        );

        return Task.CompletedTask;
    }

    private async Task DoWorkAsync()
    {
        try
        {
            await qBitContentManager.TorrentWatcher();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in torrent watcher");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

