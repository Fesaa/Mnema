using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

namespace Mnema.Providers.Managers.QBit;

internal partial class QBitContentManager(
    ILogger<QBitContentManager> logger,
    ApplicationConfiguration configuration,
    IDistributedCache cache,
    IServiceScopeFactory scopeFactory,
    IQBitClient qBitClient)
    : IContentManager, IConfigurationProvider
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

        var torrents = await qBitClient.GetTorrentsAsync(listQuery);
        if (torrents != null && torrents.Any(t => t.Hash == request.Id))
        {
            throw new MnemaException($"Torrent with hash {request.Id} has already been added");
        }

        BackgroundJob.Enqueue((Expression<Func<Task>>)(() => DownloadTorrent(request, CancellationToken.None)));
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
        finally
        {
            await cache.RemoveAsync(RequestCacheKey + request.Id);
            await messageService.DeleteContent(request.UserId, request.Id);
        }
    }

    public Task MoveToDownloadQueue(string id) => StartDownload(id);

    public async Task<bool> HasContent(Provider provider, string id)
    {
        if (!SupportedProviders.Contains(provider))
            throw new MnemaException($"Provider {provider} is not supported");

        var torrents = await GetTorrents(provider);
        return torrents.Any(c => c.Hash == id);
    }

    public async Task<IEnumerable<IContent>> GetAllContent(Provider provider)
    {
        if (!SupportedProviders.Contains(provider))
            throw new MnemaException($"Provider {provider} is not supported");

        var torrents = await GetTorrents(provider);
        if (torrents.Count == 0) return [];

        List<IContent> contents = [];

        foreach (var tInfo in torrents)
        {
            if (UploadStates.Contains(tInfo.State) && !_cleanupTorrents.ContainsKey(tInfo.Hash)) continue;

            var request = await cache.GetAsJsonAsync<DownloadRequestDto>(RequestCacheKey + tInfo.Hash);
            if (request == null) continue;

            contents.Add(new QBitTorrent(request, tInfo));
        }

        return contents;
    }

    private readonly ConcurrentDictionary<Provider, (IReadOnlyList<TorrentInfo> Torrents, DateTime CachedAt)> _torrents = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);

    private async Task<IReadOnlyList<TorrentInfo>> GetTorrents(Provider provider)
    {
        if (_torrents.TryGetValue(provider, out var cached) &&
            DateTime.UtcNow - cached.CachedAt < CacheDuration)
        {
            return cached.Torrents;
        }

        IReadOnlyList<TorrentInfo> torrents;
        try
        {
            torrents = await qBitClient.GetTorrentsAsync(new TorrentListQuery
            {
                Category = MnemaCategory,
                Tag = provider.ToString(),
            });
        }
        catch (MnemaException ex)
        {
            logger.LogTrace(ex, "Failed to load torrent list");
            return [];
        }

        _torrents[provider] = (torrents, DateTime.UtcNow);
        return torrents;
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
            },
        ]);
    }

    public Task ReloadConfiguration(CancellationToken cancellationToken)
    {
        qBitClient.Invalidate();
        return Task.CompletedTask;
    }
}
