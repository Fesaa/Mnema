using Mnema.API;

namespace Mnema.Server;

public class JobsBootstrapper(
    ISubscriptionScheduler subscriptionScheduler
    )
{

    public async Task Boostrap()
    {
        await subscriptionScheduler.EnsureScheduledAsync();
    }
    
}