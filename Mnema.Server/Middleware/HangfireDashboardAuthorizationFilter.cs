using Hangfire.Dashboard;
using Mnema.Models.Internal;

namespace Mnema.Server.Middleware;

public class HangfireDashboardAuthorizationFilter: IDashboardAuthorizationFilter
{

    public bool Authorize(DashboardContext context)
    {
        return context.GetHttpContext().User.IsInRole(Roles.HangFire);
    }
}