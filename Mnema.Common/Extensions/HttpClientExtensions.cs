using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Mnema.Common.Extensions;

public static class HttpClientExtensions
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    extension(HttpClient httpClient)
    {
        /// <summary>
        ///     Makes a head request to the url, and parses the first content type header to determine the content type
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<string?> GetContentType(string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode) return null;

            if (!response.Content.Headers.TryGetValues("Content-Type", out var contentTypeHeader)) return null;

            var contentType = contentTypeHeader.FirstOrDefault()?.Split(";").FirstOrDefault();
            if (string.IsNullOrEmpty(contentType)) return null;

            return contentType;
        }

        public async Task<Result<string, HttpRequestException>> GetCachedStringAsync(
            string url,
            IDistributedCache cache,
            DistributedCacheEntryOptions? cacheEntryOptions = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = httpClient.BaseAddress + url;

            var cachedResponse = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedResponse)) return Result<string, HttpRequestException>.Ok(cachedResponse);

            try
            {
                var response = await httpClient.GetStringAsync(url, cancellationToken);

                await cache.SetStringAsync(cacheKey, response, cacheEntryOptions ?? CacheEntryOptions, cancellationToken);

                return Result<string, HttpRequestException>.Ok(response);
            }
            catch (HttpRequestException ex)
            {
                return Result<string, HttpRequestException>.Err(ex);
            }
        }

        public async Task<Result<TResult, HttpRequestException>> GetCachedAsync<TResult>(
            string url,
            IDistributedCache cache,
            DistributedCacheEntryOptions? cacheEntryOptions = null,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = httpClient.BaseAddress + url;

            var cachedResponse = await cache.GetAsJsonAsync<TResult>(cacheKey, cancellationToken);
            if (cachedResponse != null) return Result<TResult, HttpRequestException>.Ok(cachedResponse);

            var result = await httpClient.GetAsync<TResult>(url, JsonSerializerOptions, cancellationToken);
            if (result.IsErr) return result;

            var resultValue = result.Unwrap();
            if (resultValue != null)
                await cache.SetAsJsonAsync(cacheKey, result.Unwrap(), cacheEntryOptions ?? CacheEntryOptions,
                    cancellationToken);

            return result;
        }

        public async Task<Result<TResult, HttpRequestException>> GetAsync<TResult>(string url,
            JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

                    return Result<TResult, HttpRequestException>.Err(new HttpRequestException(
                        $"Request failed with status {response.StatusCode}: {errorContent}",
                        null,
                        response.StatusCode));
                }

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var json = await JsonSerializer.DeserializeAsync<TResult>(stream, jsonSerializerOptions,
                    cancellationToken);

                if (json == null)
                    return Result<TResult, HttpRequestException>.Err(
                        new HttpRequestException("Failed to deserialize response"));

                return Result<TResult, HttpRequestException>.Ok(json);
            }
            catch (HttpRequestException ex)
            {
                return Result<TResult, HttpRequestException>.Err(ex);
            }
            catch (JsonException ex)
            {
                return Result<TResult, HttpRequestException>.Err(
                    new HttpRequestException("JSON deserialization failed", ex));
            }
        }

        public async Task<Result<TResult, HttpRequestException>> PostAsync<TResult>(
            string url, object body, JsonSerializerOptions jsonSerializerOptions,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(url, body, jsonSerializerOptions, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

                    return Result<TResult, HttpRequestException>.Err(new HttpRequestException(
                        $"Request failed with status {response.StatusCode}: {errorContent}",
                        null,
                        response.StatusCode));
                }

                var json = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken);
                if (json == null)
                    return Result<TResult, HttpRequestException>.Err(
                        new HttpRequestException("Failed to deserialize response"));

                return Result<TResult, HttpRequestException>.Ok(json);
            }
            catch (HttpRequestException ex)
            {
                return Result<TResult, HttpRequestException>.Err(ex);
            }
            catch (JsonException ex)
            {
                return Result<TResult, HttpRequestException>.Err(
                    new HttpRequestException("JSON serialization/deserialization failed", ex));
            }
        }
    }
}
