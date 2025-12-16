using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Mnema.Server.Extensions;

public static class CacheExtensions
{

    private static readonly bool IsDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development;
    
    public static IDictionary<string, CacheProfile> AddCacheProfile(
        this IDictionary<string, CacheProfile> dictionary,
        string name,
        TimeSpan duration,
        ResponseCacheLocation location = ResponseCacheLocation.Any)
    {
        // No client side caches during development
        dictionary.Add(name, new CacheProfile
        {
            Duration = IsDevelopment ? 0 : (int) duration.TotalSeconds,
            Location = location,
        });

        return dictionary;
    }

    public static OutputCacheOptions AddCachePolicy(
        this OutputCacheOptions options,
        string name,
        TimeSpan duration)
    {
        options.AddPolicy(name, b => b.Expire(duration));
        return options;
    }
    
}