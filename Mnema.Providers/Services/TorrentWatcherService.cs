using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mnema.Providers.Managers.QBit;

namespace Mnema.Providers.Services;

internal class TorrentWatcherService(ILogger<TorrentWatcherService> logger, QBitContentManager qBitContentManager)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        do
        {
            try
            {
                await qBitContentManager.TorrentWatcher(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in torrent watcher");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

