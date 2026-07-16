using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Mnema.Common.Http;

public class AutomaticRateLimitRetryHandler(
    ILogger<AutomaticRateLimitRetryHandler> logger)
    : DelegatingHandler
{
    private static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SafetyBuffer = TimeSpan.FromSeconds(5);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.TooManyRequests)
        {
            return response;
        }

        var delay = GetRetryDelay(response);
        if (delay < SafetyBuffer)
        {
            delay = SafetyBuffer;
        }

        logger.LogWarning("Received HTTP 429. Retrying after {Delay}.", delay);

        response.Dispose();

        await Task.Delay(delay, cancellationToken);

        return await base.SendAsync(request, cancellationToken);
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response)
    {
        // RFC 9110 Retry-After header
        if (response.Headers.RetryAfter is { } retryAfter)
        {
            if (retryAfter.Delta is { } delta)
            {
                return delta + SafetyBuffer;
            }

            if (retryAfter.Date is { } date)
            {
                var remaining = date - DateTimeOffset.UtcNow;
                if (remaining > TimeSpan.Zero)
                {
                    return remaining + SafetyBuffer;
                }
            }
        }

        // Common vendor-specific headers
        var headerNames = new[]
        {
            "X-RateLimit-Reset",     // Unix timestamp (seconds) is most common
            "RateLimit-Reset",       // RFC draft / some providers
            "X-Retry-After",         // Some APIs
            "Retry-After-Ms",        // Milliseconds
            "X-Retry-After-Ms"       // Milliseconds
        };

        foreach (var headerName in headerNames)
        {
            if (!response.Headers.TryGetValues(headerName, out var values))
                continue;

            var value = values.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (headerName.EndsWith("-Ms", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var ms))
            {
                return TimeSpan.FromMilliseconds(ms) + SafetyBuffer;
            }

            if (int.TryParse(value, out var seconds))
            {
                if (headerName.Contains("Reset", StringComparison.OrdinalIgnoreCase))
                {
                    var reset = DateTimeOffset.FromUnixTimeSeconds(seconds);
                    var remaining = reset - DateTimeOffset.UtcNow;

                    if (remaining > TimeSpan.Zero)
                    {
                        return remaining + SafetyBuffer;
                    }
                }
                else
                {
                    return TimeSpan.FromSeconds(seconds) + SafetyBuffer;
                }
            }
        }

        return DefaultDelay;
    }
}
