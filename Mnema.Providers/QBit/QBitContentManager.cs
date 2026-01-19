using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using QBittorrent.Client;

namespace Mnema.Providers.QBit;

internal partial class QBitContentManager(
    ILogger<QBitContentManager> logger,
    ApplicationConfiguration configuration,
    IDistributedCache cache,
    IServiceScopeFactory scopeFactory,
    IQBitClient qBitClient
    ): IContentManager, IConfigurationProvider
{
    private const string MnemaCategory = "Mnema";
    private const string UrlKey = "url";
    private const string UsernameKey = "username";
    private const string PasswordKey = "password";
    private const string RequestCacheKey = "QBitTorrent-Request-";

    private static readonly List<Provider> SupportedProviders = [Provider.Nyaa];
    private static readonly DistributedCacheEntryOptions RequestCacheKeyOptions = new();

    public async Task Download(DownloadRequestDto request)
    {
        if (!SupportedProviders.Contains(request.Provider))
            throw new MnemaException($"Provider {request.Provider} is not supported");

        if (string.IsNullOrEmpty(request.DownloadUrl))
            throw new MnemaException($"Download url is missing");

        var listQuery = new TorrentListQuery
        {
            Category = MnemaCategory,
            Tag = request.Provider.ToString(),
            Hashes = [request.Id]
        };

        try
        {
            var torrents = await qBitClient.GetTorrentsAsync(listQuery);
            if (torrents != null && torrents.Any(t => t.Hash == request.Id))
            {
                throw new MnemaException($"Torrent with hash {request.Id} has already been added");
            }

            BackgroundJob.Enqueue(() => DownloadTorrent(request, CancellationToken.None));
        }
        catch (InvalidOperationException)
        {
            // Client not available
        }
    }

    public async Task StopDownload(StopRequestDto request)
    {
        if (!SupportedProviders.Contains(request.Provider))
            throw new MnemaException($"Provider {request.Provider} is not supported");

        using var scope = scopeFactory.CreateScope();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

        try
        {
            await qBitClient.DeleteTorrentsAsync([request.Id], true);
        }
        catch (InvalidOperationException)
        {
            // Client not available
        }
        finally
        {
            await cache.RemoveAsync(RequestCacheKey + request.Id);
            await messageService.DeleteContent(request.UserId, request.Id);
        }
    }

    public Task MoveToDownloadQueue(string id) => StartDownload(id);

    public async Task<IEnumerable<IContent>> GetAllContent(Provider provider)
    {
        if (!SupportedProviders.Contains(provider))
            throw new MnemaException($"Provider {provider} is not supported");

        var listQuery = new TorrentListQuery
        {
            Category = MnemaCategory,
            Tag = provider.ToString(),
        };

        try
        {
            var torrents = await qBitClient.GetTorrentsAsync(listQuery);
            if (torrents.Count == 0) return [];

            List<IContent> contents = [];

            foreach (var tInfo in torrents)
            {
                if (UploadStates.Contains(tInfo.State)) continue;

                var request = await cache.GetAsJsonAsync<DownloadRequestDto>(RequestCacheKey + tInfo.Hash);
                if (request == null) continue;

                contents.Add(new QBitTorrent(request, tInfo));
            }

            return contents;
        }
        catch (InvalidOperationException)
        {
            return [];
        }
    }

    public Task<List<FormControlDefinition>> GetFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = UrlKey,
                Type = FormType.Text,
                Validators = new FormValidatorsBuilder()
                    .WithIsUrl()
                    .WithRequired()
                    .Build()
            },
            new FormControlDefinition
            {
                Key = UsernameKey,
                Type = FormType.Text,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .Build()
            },
            new FormControlDefinition
            {
                Key = PasswordKey,
                Type = FormType.Text,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .Build()
            },
        ]);
    }

    public Task ReloadConfiguration(CancellationToken cancellationToken)
    {
        qBitClient.Invalidate();
        return Task.CompletedTask;
    }
}
