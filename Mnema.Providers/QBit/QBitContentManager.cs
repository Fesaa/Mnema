using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    IServiceScopeFactory scopeFactory
    ): IContentManager, IConfigurationProvider
{
    private const string MnemaCategory = "Mnema";
    private const string UrlKey = "url";
    private const string UsernameKey = "username";
    private const string PasswordKey = "password";
    private const string RequestCacheKey = "QBitTorrent-Request-";

    private static readonly List<Provider> SupportedProviders = [Provider.Nyaa];
    private static readonly DistributedCacheEntryOptions RequestCacheKeyOptions = new();

    private QBittorrentClient? _qBittorrentClient;

    public async Task Download(DownloadRequestDto request)
    {
        if (!SupportedProviders.Contains(request.Provider))
            throw new MnemaException($"Provider {request.Provider} is not supported");

        if (string.IsNullOrEmpty(request.DownloadUrl))
            throw new MnemaException($"Download url is missing");

        var client = await GetQBittorrentClient();
        if (client == null)
            return;

        var listQuery = new TorrentListQuery
        {
            Category = MnemaCategory,
            Tag = request.Provider.ToString(),
            Hashes = [request.Id]
        };

        var torrents = await client.GetTorrentListAsync(listQuery);
        if (torrents != null && torrents.Any(t => t.Hash == request.Id))
        {
            throw new MnemaException($"Torrent with hash {request.Id} has already been added");
        }

        var addRequest = new AddTorrentUrlsRequest(new Uri(request.DownloadUrl))
        {
            Category = MnemaCategory,
            Tags = [request.Provider.ToString()],
            DownloadFolder = Path.Join(configuration.DownloadDir, request.BaseDir, request.TempTitle),
            Paused = !request.StartImmediately,
        };

        await client.AddTorrentsAsync(addRequest);
        await cache.SetAsJsonAsync(RequestCacheKey + request.Id, request, RequestCacheKeyOptions);
    }

    public async Task StopDownload(StopRequestDto request)
    {
        if (!SupportedProviders.Contains(request.Provider))
            throw new MnemaException($"Provider {request.Provider} is not supported");

        var client = await GetQBittorrentClient();
        if (client == null)
            return;

        using var scope = scopeFactory.CreateScope();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

        try
        {
            if (request.SaveDownload)
            {
                await CleanupTorrent(request);
            }

            await client.DeleteAsync([request.Id], true);
        }
        finally
        {
            await cache.RemoveAsync(RequestCacheKey + request.Id);
            await messageService.DeleteContent(request.UserId, request.Id);
        }
    }

    private async Task CleanupTorrent(StopRequestDto request)
    {

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

        var client = await GetQBittorrentClient();
        if (client == null)
            return [];

        var torrents = await client.GetTorrentListAsync(listQuery);
        if (torrents == null) return [];

        List<IContent> contents = [];

        foreach (var tInfo in torrents)
        {
            if (tInfo.State == TorrentState.Uploading) continue;

            var request = await cache.GetAsJsonAsync<DownloadRequestDto>(RequestCacheKey + tInfo.Hash);
            if (request == null) continue;

            contents.Add(new QBitTorrent(request, tInfo));
        }

        return contents;
    }

    private async Task<IQBittorrentClient2?> GetQBittorrentClient()
    {
        if (_qBittorrentClient != null)
            return _qBittorrentClient;

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var downloadClient = await unitOfWork.DownloadClientRepository
            .GetDownloadClientAsync(DownloadClientType.QBittorrent, CancellationToken.None);

        if (downloadClient == null)
            return null;

        var url = downloadClient.Metadata.GetString(UrlKey);
        var username = downloadClient.Metadata.GetString(UsernameKey);
        var password = downloadClient.Metadata.GetString(PasswordKey);
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            logger.LogDebug("An incomplete configuration was provided for {DownloadClientType}",
                DownloadClientType.QBittorrent);
            return null;
        }

        _qBittorrentClient = new QBittorrentClient(new Uri(url));
        await _qBittorrentClient.LoginAsync(username, password);

        EnsureWatcherInitialized();

        return _qBittorrentClient;
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
        _qBittorrentClient = null;
        return Task.CompletedTask;
    }
}
