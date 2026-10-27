using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.Entities.Content;

namespace Mnema.Providers.Managers.QBit;

internal class QBitClient(
    ILogger<QBitClient> logger,
    IServiceScopeFactory scopeFactory
) : IQBitClient
{
    private static readonly IMetadataKey<string?> UrlKey = MetadataKeys.OptionalString("url");
    private static readonly IMetadataKey<string?> UsernameKey = MetadataKeys.OptionalString("username");
    private static readonly IMetadataKey<string?> PasswordKey = MetadataKeys.OptionalString("password");
    private static readonly Version StartStopApiVersion = new(2, 11, 0);

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SemaphoreSlim _authSemaphore = new(1, 1);

    private HttpClient? _client;
    private DownloadClient? _downloadClientMetadata;
    private string? _username;
    private string? _password;
    private Version _webApiVersion = new(2, 0, 0);
    private bool _isInitialized;

    public Task<IReadOnlyList<TorrentInfo>> GetTorrentsAsync(TorrentListQuery? query = null, CancellationToken token = default)
    {
        return ExecuteAsync(async client =>
        {
            var body = await SendAsync(client, HttpMethod.Get, BuildTorrentListUri(query), null, token);
            var torrents = JsonSerializer.Deserialize<List<TorrentInfo>>(body) ?? [];

            IReadOnlyList<TorrentInfo> result = torrents;
            return result;
        });
    }

    public async Task AddTorrentsAsync(AddTorrentUrlsRequest request, CancellationToken token = default)
    {
        await ExecuteAsync(async client =>
        {
            var parameters = new Dictionary<string, string>
            {
                ["urls"] = string.Join("\n", request.TorrentUrls.Select(url => url.AbsoluteUri)),
            };

            if (!string.IsNullOrEmpty(request.DownloadFolder))
            {
                parameters["savepath"] = request.DownloadFolder;
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                parameters["category"] = request.Category;
            }

            if (request.Tags != null)
            {
                parameters["tags"] = string.Join(",", request.Tags);
            }

            if (request.Paused)
            {
                // qBittorrent 5.0 renamed this parameter to "stopped"; older versions ignore the unknown field
                parameters["paused"] = "true";
                parameters["stopped"] = "true";
            }

            await SendAsync(client, HttpMethod.Post, "api/v2/torrents/add", parameters, token);
        });
    }

    public async Task DeleteTorrentsAsync(IEnumerable<string> hashes, bool deleteFiles, CancellationToken token = default)
    {
        await ExecuteAsync(async client =>
        {
            var parameters = new Dictionary<string, string>
            {
                ["hashes"] = string.Join("|", hashes),
                ["deleteFiles"] = deleteFiles ? "true" : "false",
            };

            await SendAsync(client, HttpMethod.Post, "api/v2/torrents/delete", parameters, token);
        });
    }

    public Task<IReadOnlyList<TorrentContent>> GetTorrentContentsAsync(string hash, CancellationToken token = default)
    {
        return ExecuteAsync(async client =>
        {
            var body = await SendAsync(client, HttpMethod.Get, $"api/v2/torrents/files?hash={Uri.EscapeDataString(hash)}", null, token);
            var contents = JsonSerializer.Deserialize<List<TorrentContent>>(body) ?? [];

            IReadOnlyList<TorrentContent> result = contents;
            return result;
        });
    }

    public async Task SetFilePriorityAsync(string hash, IEnumerable<int> indices, TorrentContentPriority priority, CancellationToken token = default)
    {
        await ExecuteAsync(async client =>
        {
            var parameters = new Dictionary<string, string>
            {
                ["hash"] = hash,
                ["id"] = string.Join("|", indices),
                ["priority"] = ((int)priority).ToString(),
            };

            await SendAsync(client, HttpMethod.Post, "api/v2/torrents/filePrio", parameters, token);
        });
    }

    public async Task ResumeTorrentsAsync(IEnumerable<string> hashes, CancellationToken token = default)
    {
        await ExecuteAsync(async client =>
        {
            var parameters = new Dictionary<string, string>
            {
                ["hashes"] = string.Join("|", hashes),
            };

            var endpoint = _webApiVersion >= StartStopApiVersion
                ? "api/v2/torrents/start"
                : "api/v2/torrents/resume";

            await SendAsync(client, HttpMethod.Post, endpoint, parameters, token);
        });
    }

    public void Invalidate()
    {
        _isInitialized = false;
        _client?.Dispose();
        _client = null;
        _downloadClientMetadata = null;
        _username = null;
        _password = null;
        _webApiVersion = new Version(2, 0, 0);
    }

    private async Task<T> ExecuteAsync<T>(Func<HttpClient, Task<T>> action)
    {
        var client = await GetClientAsync();
        if (client == null) throw new MnemaException("qBittorrent client is not available.");

        try
        {
            return await action(client);
        }
        catch (Exception ex) when (ex is HttpRequestException or MnemaException)
        {
            logger.LogWarning(ex, "qBittorrent request failed. Invalidating client.");
            await HandleFailure();
            throw;
        }
    }

    private async Task ExecuteAsync(Func<HttpClient, Task> action)
    {
        await ExecuteAsync(async client =>
        {
            await action(client);
            return true;
        });
    }

    private async Task<string> SendAsync(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        IDictionary<string, string>? parameters,
        CancellationToken token)
    {
        using var response = await SendOnceAsync(client, method, requestUri, parameters, token);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            await LoginAsync(client, _username!, _password!, token);

            using var retry = await SendOnceAsync(client, method, requestUri, parameters, token);
            return await ReadResponseAsync(retry, token);
        }

        return await ReadResponseAsync(response, token);
    }

    private static async Task<HttpResponseMessage> SendOnceAsync(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        IDictionary<string, string>? parameters,
        CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        if (parameters != null)
        {
            request.Content = new FormUrlEncodedContent(parameters);
        }

        return await client.SendAsync(request, token);
    }

    private static async Task<string> ReadResponseAsync(HttpResponseMessage response, CancellationToken token)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(token);
    }

    private async Task LoginAsync(HttpClient client, string username, string password, CancellationToken token)
    {
        await _authSemaphore.WaitAsync(token);
        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password,
            });

            using var response = await client.PostAsync("api/v2/auth/login", content, token);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new MnemaException("qBittorrent rejected the configured username or password.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new MnemaException("qBittorrent rejected the login with 403 Forbidden. Check the WebUI IP ban list.");
            }

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(token);
            if (body.Trim() == "Fails.")
            {
                throw new MnemaException("qBittorrent rejected the configured username or password.");
            }
        }
        finally
        {
            _authSemaphore.Release();
        }
    }

    private async Task HandleFailure()
    {
        await _semaphore.WaitAsync();
        try
        {
            var clientId = _downloadClientMetadata?.Id;
            Invalidate();

            if (clientId.HasValue)
            {
                using var scope = scopeFactory.CreateScope();
                var downloadClientService = scope.ServiceProvider.GetRequiredService<IDownloadClientService>();
                await downloadClientService.MarkAsFailed(clientId.Value, CancellationToken.None);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<HttpClient?> GetClientAsync()
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
                logger.LogTrace("Download client {Id} is in a failed state until {Until}",
                    downloadClient.Id, downloadClient.FailedAt?.AddHours(1));
                return null;
            }

            var url = downloadClient.Metadata.GetKey(UrlKey);
            var username = downloadClient.Metadata.GetKey(UsernameKey);
            var password = downloadClient.Metadata.GetKey(PasswordKey);

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                logger.LogDebug("An incomplete configuration was provided for {DownloadClientType}",
                    DownloadClientType.QBittorrent);
                return null;
            }

            HttpClient? client = null;
            try
            {
                var handler = new HttpClientHandler
                {
                    // Keeps whatever cookie login sets, including 5.2's dynamically named QBT_SID_<port>
                    CookieContainer = new CookieContainer(),
                    UseCookies = true,
                };

                client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url.TrimEnd('/') + "/"),
                };

                await LoginAsync(client, username, password, CancellationToken.None);
                _webApiVersion = await GetWebApiVersionAsync(client, CancellationToken.None);

                _client = client;
                _downloadClientMetadata = downloadClient;
                _username = username;
                _password = password;
                _isInitialized = true;

                return _client;
            }
            catch (Exception e)
            {
                client?.Dispose();
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

    private static async Task<Version> GetWebApiVersionAsync(HttpClient client, CancellationToken token)
    {
        using var response = await client.GetAsync("api/v2/app/webapiVersion", token);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(token);
        return Version.TryParse(body.Trim(), out var version) ? version : new Version(2, 0, 0);
    }

    private string BuildTorrentListUri(TorrentListQuery? query)
    {
        if (query == null) return "api/v2/torrents/info";

        var parameters = new Dictionary<string, string>();

        if (query.Filter != TorrentListFilter.All)
        {
            parameters["filter"] = query.Filter switch
            {
                TorrentListFilter.Paused => _webApiVersion >= StartStopApiVersion ? "stopped" : "paused",
                TorrentListFilter.Resumed => _webApiVersion >= StartStopApiVersion ? "running" : "resumed",
                TorrentListFilter.StalledUploading => "stalled_uploading",
                TorrentListFilter.StalledDownloading => "stalled_downloading",
                _ => query.Filter.ToString().ToLowerInvariant(),
            };
        }

        if (query.Category != null)
        {
            parameters["category"] = query.Category;
        }

        if (query.SortBy != null)
        {
            parameters["sort"] = query.SortBy;
        }

        if (query.ReverseSort)
        {
            parameters["reverse"] = "true";
        }

        if (query.Limit.HasValue)
        {
            parameters["limit"] = query.Limit.Value.ToString();
        }

        if (query.Offset.HasValue)
        {
            parameters["offset"] = query.Offset.Value.ToString();
        }

        if (query.Hashes != null)
        {
            parameters["hashes"] = string.Join("|", query.Hashes);
        }

        if (query.Tag != null)
        {
            parameters["tag"] = query.Tag;
        }

        var queryString = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        return queryString.Length == 0
            ? "api/v2/torrents/info"
            : $"api/v2/torrents/info?{queryString}";
    }

    public void Dispose()
    {
        _client?.Dispose();
        _semaphore.Dispose();
        _authSemaphore.Dispose();
    }
}
