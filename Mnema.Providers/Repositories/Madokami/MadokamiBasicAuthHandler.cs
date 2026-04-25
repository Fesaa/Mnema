using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Models.Entities.Content;

namespace Mnema.Providers.Repositories.Madokami;

internal class MadokamiBasicAuthHandler(IUnitOfWork unitOfWork, IDistributedCache cache): DelegatingHandler
{

    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string authHeader;
        var cachedHeader = await cache.GetStringAsync(MadokamiRepository.BasicAuthCacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedHeader))
        {
            authHeader = cachedHeader;
        }
        else
        {
            authHeader = await GetAuthHeader(cancellationToken);
            await cache.SetStringAsync(MadokamiRepository.BasicAuthCacheKey, authHeader, CacheEntryOptions, cancellationToken);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAuthHeader(CancellationToken cancellationToken)
    {
        var settings =
            await unitOfWork.DownloadClientRepository.GetDownloadClientAsync(DownloadClientType.Madokami,
                cancellationToken);

        if (settings == null)
        {
            throw new MnemaException("Madokami isn't configured");
        }

        var username = settings.Metadata.GetKey(MadokamiRepository.BasicAuthUsername);
        var password = settings.Metadata.GetKey(MadokamiRepository.BasicAuthPassword);

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            throw new MnemaException("Madokami isn't configured (username or password missing)");
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
    }
}
