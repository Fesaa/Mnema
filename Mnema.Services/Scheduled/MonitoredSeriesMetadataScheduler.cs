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
    private const int FetchDelay = 100;
    private static readonly RecurringJobOptions RecurringJobOptions = new()
    {
        TimeZone = TimeZoneInfo.Local
    };

    public Task EnsureScheduledAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Registering Monitored Series metadata refresher @ {CronJob}", CronJob);

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
        var connectionService = scope.ServiceProvider.GetRequiredService<IConnectionService>();

        var series = await unitOfWork.MonitoredSeriesRepository.GetSeriesEligibleForRefresh(ct);

        if (series.Count == 0)
            return;

        logger.LogDebug("{Amount} series eligible for refresh", series.Count);

        var sw = Stopwatch.StartNew();
        var failures = 0;

        foreach (var mSeries in series)
        {
            logger.LogDebug("Refreshing metadata for {Title} - {Provider}", mSeries.Title, mSeries.Provider);

            try
            {
                await monitoredSeriesService.EnrichWithMetadata(mSeries.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh metadata for {Title} - {Provider}", mSeries.Title, mSeries.Provider);
                failures++;

                connectionService.CommunicateException($"Failed to refresh metadata for {mSeries.Title} - {mSeries.Provider}", ex);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(FetchDelay), ct);
        }

        logger.LogInformation("Refreshed metadata for {Amount} series in {Elapsed}ms",
            series.Count, sw.Elapsed.TotalMilliseconds - FetchDelay * series.Count);

        if (failures > 0)
            logger.LogWarning("Failed to refresh metadata for {Amount} series", failures);
    }
}
