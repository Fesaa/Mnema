using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Enums;
using Mnema.Models.Internal;
using QBittorrent.Client;

namespace Mnema.Providers.Managers.QBit;

internal partial class QBitContentManager(
    ILogger<QBitContentManager> logger,
    ApplicationConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    IQBitClient qBitClient)
    : IContentManager, IConfigurationProvider
{
    private const string MnemaCategory = "Mnema";
    private const string UrlKey = "url";
    private const string UsernameKey = "username";
    private const string PasswordKey = "password";

    private static readonly List<Provider> SupportedProviders = [Provider.Nyaa];

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
        if (torrents.Any(t => t.Hash == request.Id) && !request.GetKey(RequestConstants.IsGroupedDownload))
        {
            throw new MnemaException($"Torrent with hash {request.Id} has already been added");
        }

        if (request.GetKey(RequestConstants.IsGroupedDownload))
        {
            await ValidateNoDuplicateSeriesInGroupedTorrent(request);
        }

        BackgroundJob.Enqueue((Expression<Func<Task>>)(() => DownloadTorrent(request, CancellationToken.None)));
    }

    private async Task ValidateNoDuplicateSeriesInGroupedTorrent(DownloadRequestDto request)
    {
        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var hardcoverId = request.GetKey(RequestConstants.HardcoverSeriesIdKey);
        var mangaBakaId = request.GetKey(RequestConstants.MangaBakaKey);

        if (string.IsNullOrEmpty(hardcoverId) && string.IsNullOrEmpty(mangaBakaId))
        {
            throw new BadRequestException($"Grouped downloads must contain external metadata");
        }

        var externalDownloads = await unitOfWork.ExternalDownloadRepository.GetByExternalId(request.Id);

        var alreadyBeingDownloaded = externalDownloads.Any(ed =>
        {
            var edHardcoverId = ed.GetKey(RequestConstants.HardcoverSeriesIdKey);
            var edMangaBakaId = ed.GetKey(RequestConstants.MangaBakaKey);

            return (!string.IsNullOrEmpty(edHardcoverId) && edHardcoverId == hardcoverId)
                || (!string.IsNullOrEmpty(edMangaBakaId) && edMangaBakaId == mangaBakaId);
        });

        if (alreadyBeingDownloaded)
        {
            throw new BadRequestException($"A download for Hardcover({hardcoverId}) or MangaBaka({mangaBakaId}) is already being downloaded. Cannot queue the same series");
        }
    }

    public async Task StopDownload(StopRequestDto request)
    {
        if (!SupportedProviders.Contains(request.Provider))
            throw new MnemaException($"Provider {request.Provider} is not supported");

        using var scope = scopeFactory.CreateScope();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var externalDownload = await GetExternalDownload(request.Id, CancellationToken.None);

        if (!request.DeleteFromDownloadClient)
        {
            await unitOfWork.ExternalDownloadRepository.DeleteById(externalDownload.Id);
            await messageService.DeleteContent(request.Id);
            return;
        }

        var allDownloads = await unitOfWork.ExternalDownloadRepository.GetByExternalId(externalDownload.ExternalId);

        if (allDownloads.Count > 1)
        {
            logger.LogDebug("More than one external download found for hash {Hash}, stopping download of files instead", externalDownload.ExternalId);

            await FilterContent(externalDownload.ExternalId, currentlySelected => currentlySelected
                .Except(externalDownload.Files.Select(f => f.FullPath))
                .ToList(), CancellationToken.None);
        }
        else
        {
            await qBitClient.DeleteTorrentsAsync([externalDownload.ExternalId], true);
        }

        await unitOfWork.ExternalDownloadRepository.DeleteById(externalDownload.Id);
        await messageService.DeleteContent(request.Id);
    }

    public async Task<bool> HasContent(Provider provider, string id)
    {
        if (!SupportedProviders.Contains(provider))
            throw new MnemaException($"Provider {provider} is not supported");

        var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        return await unitOfWork.ExternalDownloadRepository.ExistsByExternalId(id);
    }

    public async Task<IEnumerable<IContent>> GetAllContent(Provider provider)
    {
        if (!SupportedProviders.Contains(provider))
            throw new MnemaException($"Provider {provider} is not supported");

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var torrents = await GetTorrents(provider);
        if (torrents.Count == 0) return [];

        List<IContent> contents = [];

        var externalDownloads = await unitOfWork.ExternalDownloadRepository.GetAll(CancellationToken.None);
        foreach (var externalDownload in externalDownloads)
        {
            var torrent = torrents.FirstOrDefault(t => t.Hash == externalDownload.ExternalId);
            if (torrent == null)
            {
                logger.LogWarning("External download has no linked torrent: {Id}", externalDownload.Id);
                continue;
            }

            contents.Add(new ExternalDownloadContent(externalDownload, torrent));
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

    public Task<List<FormFieldDefinition>> GetFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormFieldDefinition>>([
            new TextFieldDefinition
            {
                Key = UrlKey,
                Validators = new FormValidatorsBuilder()
                    .WithIsUrl()
                    .WithRequired()
                    .Build()
            },
            new TextFieldDefinition
            {
                Key = UsernameKey,
                Validators = FormValidatorsBuilder.Required
            },
            new TextFieldDefinition
            {
                Key = PasswordKey,
            },
        ]);
    }

    public Task ReloadConfiguration(CancellationToken cancellationToken)
    {
        qBitClient.Invalidate();
        return Task.CompletedTask;
    }
}
