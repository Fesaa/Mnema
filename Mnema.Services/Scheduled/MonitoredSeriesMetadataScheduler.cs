using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;

namespace Mnema.Services.Scheduled;

internal class MonitoredSeriesMetadataScheduler(
    ILogger<MonitoredSeriesMetadataScheduler> logger,
    IServiceScopeFactory scopeFactory,
    IRecurringJobManagerV2 recurringJobManager
): IScheduled
{
    private const string JobId = "monitored-series.metadata";
    private const string CronJob = "0 1 * * *";
    private static readonly RecurringJobOptions RecurringJobOptions = new()
    {
        TimeZone = TimeZoneInfo.Local
    };

    public Task EnsureScheduledAsync()
    {
        logger.LogDebug("Registering Monitored Series metadata refresher");

        recurringJobManager.AddOrUpdate<MonitoredSeriesMetadataScheduler>(JobId,
            s => s.ReloadMetadataAsync(CancellationToken.None),
            CronJob, RecurringJobOptions);

        return Task.CompletedTask;
    }

    public async Task ReloadMetadataAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var monitoredSeriesService = scope.ServiceProvider.GetRequiredService<IMonitoredSeriesService>();

        var series = await unitOfWork.MonitoredSeriesRepository.GetSeriesEligibleForRefresh(ct);

        if (series.Count == 0)
            return;

        logger.LogDebug("{Amount} series eligible for refresh", series.Count);

        var sw = Stopwatch.StartNew();

        foreach (var mSeries in series)
        {
            logger.LogDebug("Refreshing metadata for {Title}", mSeries.Title);

            await monitoredSeriesService.EnrichWithMetadata(mSeries, ct);

            await unitOfWork.CommitAsync(ct);

            await Task.Delay(TimeSpan.FromMilliseconds(10), ct);
        }

        logger.LogInformation("Refreshed metadata for {Amount} series in {Elapsed}ms",
            series.Count, sw.Elapsed.TotalMilliseconds - 10 * series.Count);
    }
}
