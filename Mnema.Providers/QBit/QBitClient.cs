using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using QBittorrent.Client;

namespace Mnema.Providers.QBit;

internal class QBitClient(
    ILogger<QBitClient> logger,
    IServiceScopeFactory scopeFactory
) : IQBitClient
{
    private const string UrlKey = "url";
    private const string UsernameKey = "username";
    private const string PasswordKey = "password";

    private QBittorrentClient? _client;
    private DownloadClient? _downloadClientMetadata;
    private bool _isInitialized;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IReadOnlyList<TorrentInfo>> GetTorrentsAsync(TorrentListQuery? query = null, CancellationToken token = default)
    {
        return await ExecuteAsync(c => c.GetTorrentListAsync(query, token));
    }

    public async Task AddTorrentsAsync(AddTorrentUrlsRequest request, CancellationToken token = default)
    {
        await ExecuteAsync(c => c.AddTorrentsAsync(request, token));
    }

    public async Task DeleteTorrentsAsync(IEnumerable<string> hashes, bool deleteFiles, CancellationToken token = default)
    {
        await ExecuteAsync(c => c.DeleteAsync(hashes, deleteFiles, token));
    }

    public async Task<IReadOnlyList<TorrentContent>> GetTorrentContentsAsync(string hash, CancellationToken token = default)
    {
        return await ExecuteAsync(c => c.GetTorrentContentsAsync(hash, token));
    }

    public async Task SetFilePriorityAsync(string hash, IEnumerable<int> indices, TorrentContentPriority priority, CancellationToken token = default)
    {
        await ExecuteAsync(c => c.SetFilePriorityAsync(hash, indices, priority, token));
    }

    public async Task ResumeTorrentsAsync(IEnumerable<string> hashes, CancellationToken token = default)
    {
        await ExecuteAsync(c => c.ResumeAsync(hashes, token));
    }

    public void Invalidate()
    {
        _isInitialized = false;
        _client?.Dispose();
        _client = null;
        _downloadClientMetadata = null;
    }

    private async Task<T> ExecuteAsync<T>(Func<QBittorrentClient, Task<T>> action)
    {
        var client = await GetClientAsync();
        if (client == null) throw new MnemaException("qBittorrent client is not available.");

        try
        {
            return await action(client);
        }
        catch (Exception ex) when (ex is HttpRequestException or QBittorrentClientRequestException)
        {
            logger.LogWarning(ex, "qBittorrent request failed. Invalidating client.");
            await HandleFailure(ex);
            throw;
        }
    }

    private async Task ExecuteAsync(Func<QBittorrentClient, Task> action)
    {
        var client = await GetClientAsync();
        if (client == null) throw new InvalidOperationException("qBittorrent client is not available.");

        try
        {
            await action(client);
        }
        catch (Exception ex) when (ex is HttpRequestException or QBittorrentClientRequestException)
        {
            logger.LogWarning(ex, "qBittorrent request failed. Invalidating client.");
            await HandleFailure(ex);
            throw;
        }
    }

    private async Task HandleFailure(Exception ex)
    {
        Guid? clientId = null;
        lock (_semaphore)
        {
            clientId = _downloadClientMetadata?.Id;
            Invalidate();
        }

        if (clientId.HasValue)
        {
            using var scope = scopeFactory.CreateScope();
            var downloadClientService = scope.ServiceProvider.GetRequiredService<IDownloadClientService>();
            await downloadClientService.MarkAsFailed(clientId.Value, CancellationToken.None);
        }
    }

    private async Task<QBittorrentClient?> GetClientAsync()
    {
        if (_isInitialized && _client != null) return _client;

        await _semaphore.WaitAsync();
        try
        {
            if (_isInitialized && _client != null) return _client;

            using var scope = scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var downloadClient = await unitOfWork.DownloadClientRepository
                .GetDownloadClientAsync(DownloadClientType.QBittorrent, CancellationToken.None);

            if (downloadClient == null) return null;

            if (downloadClient.IsFailed)
            {
                logger.LogDebug("Download client {Id} is in a failed state until {Until}",
                    downloadClient.Id, downloadClient.FailedAt?.AddHours(1));
                return null;
            }

            var url = downloadClient.Metadata.GetString(UrlKey);
            var username = downloadClient.Metadata.GetString(UsernameKey);
            var password = downloadClient.Metadata.GetString(PasswordKey);

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                logger.LogDebug("An incomplete configuration was provided for {DownloadClientType}",
                    DownloadClientType.QBittorrent);
                return null;
            }

            try
            {
                var client = new QBittorrentClient(new Uri(url));
                await client.LoginAsync(username, password);
                _client = client;
                _downloadClientMetadata = downloadClient;
                _isInitialized = true;
                return _client;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to connect to the configured qBittorrent client");
                var downloadClientService = scope.ServiceProvider.GetRequiredService<IDownloadClientService>();
                await downloadClientService.MarkAsFailed(downloadClient.Id, CancellationToken.None);
                return null;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _semaphore.Dispose();
    }
}
