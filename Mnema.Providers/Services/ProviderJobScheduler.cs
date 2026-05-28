using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Providers.Dynasty;

namespace Mnema.Providers.Services;

internal class ProviderJobScheduler(ILogger<ProviderJobScheduler> logger, IRecurringJobManagerV2 recurringJobManager): IScheduled
{
    private const string DynastyJobName = "dynasty.correct-ids";

    public Task EnsureScheduledAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Scheduling re-occuring jobs for providers");

        recurringJobManager.AddOrUpdate<DynastyRepository>(DynastyJobName,
            r => r.CorrectDynastyIds(CancellationToken.None),
            Cron.Weekly, new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

        return Task.CompletedTask;
    }
}
