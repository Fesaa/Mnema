using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Mnema.Common.Extensions;

public static class DistributedCacheExtensions
{
    extension(IDistributedCache cache)
    {
        public Task SetAsJsonAsync<T>(string key,
            T value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default
        )
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            return cache.SetAsync(key, bytes, options, token);
        }

        public void SetAsJson<T>(string key, T value, DistributedCacheEntryOptions options)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            cache.Set(key, bytes, options);
        }

        public async Task<T?> GetAsJsonAsync<T>(string key,
            CancellationToken token = default)
        {
            var bytes = await cache.GetAsync(key, token);
            if (bytes == null) return default;

            return JsonSerializer.Deserialize<T>(bytes);
        }

        public T? GetAsJson<T>(string key)
        {
            var bytes = cache.Get(key);
            if (bytes == null) return default;

            return JsonSerializer.Deserialize<T>(bytes);
        }
    }
}
