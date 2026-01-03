using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mnema.API;

namespace Mnema.Server;

public class JobsBootstrapper(IServiceScopeFactory scopeFactory): IHostedService
{

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        
        var subscriptionSchedular = scope.ServiceProvider.GetRequiredService<ISubscriptionScheduler>();
        await subscriptionSchedular.EnsureScheduledAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}