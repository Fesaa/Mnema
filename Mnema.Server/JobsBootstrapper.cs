using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mnema.API;

namespace Mnema.Server;

public class JobsBootstrapper(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobsBootstrapper>>();

        var storage = JobStorage.Current;
        using var con = storage.GetConnection();
        var servers = con?.RemoveTimedOutServers(TimeSpan.FromSeconds(5));

        logger.LogInformation("Removed timed out servers: {Servers}", servers);


        var schedulers = scope.ServiceProvider.GetServices<IScheduled>();

        foreach (var scheduler in schedulers)
        {
            await scheduler.EnsureScheduledAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
