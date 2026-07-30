using Microsoft.Extensions.DependencyInjection;

namespace Mnema.Common.Extensions;

public static class IServiceScopeExtensions
{
    public static T GetRequiredService<T>(this IServiceScope scope)
    where T : notnull
    {
        return scope.ServiceProvider.GetRequiredService<T>();
    }
}
