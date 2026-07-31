using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using QBittorrent.Client;

namespace Mnema.Providers.Managers.QBit;

internal partial class QBitContentManager
{
    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task DownloadTorrent(DownloadRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.DownloadUrl))
            return;

        using var scope = scopeFactory.CreateScope();
        var services = ResolveServices(scope.ServiceProvider);

        var series = await services.MetadataResolver.ResolveSeriesAsync(request.Provider, request.Metadata, ct);
        var title = ResolveTitle(request, series, services.ParserService);

        if (string.IsNullOrEmpty(title))
        {
            logger.LogWarning("[{Id}] Downloaded content has no title, aborting download", request.Id);
            return;
        }

        var mSeries = request.GetKey(RequestConstants.MonitoredSeriesId) is { } monitoredSeriesId
            ? await services.UnitOfWork.MonitoredSeriesRepository.GetById(monitoredSeriesId, ct: ct)
            : null;

        var torrentInfo = await services.ScannerService.ParseTorrentFile(request.DownloadUrl, ct);

        var normalizedTitles = GetNormalizedTitles(title, series);
        var seriesFiles = ParseSeriesFiles(request, torrentInfo.Files, normalizedTitles, services.ParserService);
        var toDownload = FilterFilesToDownload(request, title, seriesFiles, mSeries, services, ct);

        if (toDownload.Count == 0)
        {
            logger.LogDebug("[{Title}/{Id}] No files to download, skipping", title, request.Id);
            return;
        }

        logger.LogDebug("[{Title}/{Id}] Found {Count}/{TotalCount} files to download",
            title, request.Id, toDownload.Count, seriesFiles.Count);

        var newDownload = await EnsureTorrentAddedAsync(request, title, ct);

        var externalDownload = await SaveExternalDownloadRecord(services.UnitOfWork, request, title, seriesFiles, toDownload, ct);

        try
        {
            if (toDownload.Count != torrentInfo.Files.Count)
            {
                await ApplyTorrentFileFiltersAsync(request.Id, seriesFiles, toDownload, newDownload, ct);
            }

            if (request.StartImmediately)
            {
                await qBitClient.ResumeTorrentsAsync([request.Id], ct);
            }

            await BroadcastDownloadStartedAsync(services, request, series, externalDownload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to filter or start download. Aborting");

            await services.UnitOfWork.ExternalDownloadRepository.DeleteById(externalDownload.Id, ct);
        }
    }

    #region Helper Methods

    internal static ResolvedServices ResolveServices(IServiceProvider sp) => new(
        sp.GetRequiredService<IMetadataResolver>(),
        sp.GetRequiredService<IParserService>(),
        sp.GetRequiredService<IScannerService>(),
        sp.GetRequiredService<IConnectionService>(),
        sp.GetRequiredService<IMessageService>(),
        sp.GetRequiredService<IUnitOfWork>()
    );

    internal record ResolvedServices(
        IMetadataResolver MetadataResolver,
        IParserService ParserService,
        IScannerService ScannerService,
        IConnectionService ConnectionService,
        IMessageService MessageService,
        IUnitOfWork UnitOfWork);

    internal static string? ResolveTitle(DownloadRequestDto request, Series? series, IParserService parser)
    {
        var cFormat = request.Metadata.GetKey(RequestConstants.ContentFormatKey);
        return request.Metadata.GetKey(RequestConstants.TitleOverride)
            .OrNonEmpty(series?.Title, parser.ParseSeries(request.TempTitle, cFormat), request.TempTitle);
    }

    internal static HashSet<string> GetNormalizedTitles(string title, Series? series)
    {
        return new[] { title, series?.Title, series?.LocalizedSeries }
            .WhereNotNull()
            .Select(t => t.ToNormalized())
            .ToHashSet();
    }

    internal static List<ParsedTorrentFile> ParseSeriesFiles(DownloadRequestDto request, IEnumerable<TorrentFile> files, HashSet<string> normalizedTitles, IParserService parser)
    {
        var cFormat = request.Metadata.GetKey(RequestConstants.ContentFormatKey);
        var isGrouped = request.GetKey(RequestConstants.IsGroupedDownload);

        return files
            .Select(f => new ParsedTorrentFile(f, parser.FullParse(f.FileName, cFormat)))
            .WhereIf(isGrouped, pair => pair.ParseResult.Series.Any(s => normalizedTitles.Contains(s.ToNormalized())))
            .ToList();
    }

    internal List<ParsedTorrentFile> FilterFilesToDownload(DownloadRequestDto request, string title, List<ParsedTorrentFile> seriesFiles, MonitoredSeries? mSeries, ResolvedServices services, CancellationToken ct)
    {
        var downloadDir = Path.Join(request.BaseDir, title);
        var existingContent = services.ScannerService.ScanDirectory(
            downloadDir,
            request.Metadata.GetKey(RequestConstants.ContentFormatKey),
            request.Metadata.GetKey(RequestConstants.FormatKey), ct);

        var ignoreNonMatched = request.GetKey(RequestConstants.IgnoreNonMatchedVolumes);

        return seriesFiles
            .WhereIf(mSeries != null, pair => ShouldDownloadMonitoredFile(pair, mSeries!, ignoreNonMatched, title, request.Id, services.ParserService))
            .Where(pair => IsFileNew(pair, existingContent, title, request.Id, services.ParserService))
            .ToList();
    }

    internal bool ShouldDownloadMonitoredFile(ParsedTorrentFile pair, MonitoredSeries mSeries, bool ignoreNonMatched, string title, string requestId, IParserService parserService)
    {
        var mChapter = parserService.FindMatch(mSeries.Chapters, pair.ParseResult);
        if (mChapter?.Status == MonitoredChapterStatus.NotMonitored)
        {
            logger.LogTrace("[{Title}/{Id}] Not downloading {FileName}: Chapter not monitored", title, requestId, pair.File.FileName);
            return false;
        }

        if (mChapter == null && ignoreNonMatched && mSeries.Chapters.Count > 0)
        {
            logger.LogTrace("[{Title}/{Id}] Not downloading {FileName}: Unmatched chapter", title, requestId, pair.File.FileName);
            return false;
        }

        return true;
    }

    internal bool IsFileNew(ParsedTorrentFile pair, List<OnDiskContent> existingContent, string title, string requestId, IParserService parserService)
    {
        var match = parserService.FindMatch(existingContent, pair.ParseResult);
        if (match == null)
        {
            logger.LogTrace("[{Title}/{Id}] Found new chapter to download: {FileName} - {ParseResult}", title, requestId, pair.File.FileName, pair.ParseResult);
            return true;
        }

        logger.LogTrace("[{Title}/{Id}] Not downloading {FileName}: Already exists as {FileOnDisk}", title, requestId, pair.File.FileName, match.FileName);
        return false;
    }

    internal async Task<bool> EnsureTorrentAddedAsync(DownloadRequestDto request, string title, CancellationToken ct)
    {
        var listRequest = new TorrentListQuery
        {
            Category = MnemaCategory,
            Hashes = [request.Id],
            Tag = request.Provider.ToString(),
        };

        var torrents = await qBitClient.GetTorrentsAsync(listRequest, ct);
        if (torrents.Count == 0)
        {
            if (string.IsNullOrEmpty(request.DownloadUrl))
            {
                throw new MnemaException("No download url found, cannot start torrent download");
            }

            var addRequest = new AddTorrentUrlsRequest(new Uri(request.DownloadUrl))
            {
                Category = MnemaCategory,
                Tags = [request.Provider.ToString()],
                DownloadFolder = Path.Join(configuration.DownloadDir, request.BaseDir),
                Paused = true,
            };

            await qBitClient.AddTorrentsAsync(addRequest, ct);
        }

        return torrents.Count == 0;
    }

    internal static async Task<ExternalDownload> SaveExternalDownloadRecord(IUnitOfWork uow, DownloadRequestDto request, string title, List<ParsedTorrentFile> seriesFiles, List<ParsedTorrentFile> toDownload, CancellationToken ct)
    {
        var externalDownload = new ExternalDownload
        {
            ExternalId = request.Id,
            Title = title,
            Provider = request.Provider,
            UserId = request.UserId,
            Metadata = request.Metadata,
            BaseDir = request.BaseDir,
            Files = seriesFiles.Select(pair => new ExternalDownloadFile
            {
                FileName = pair.File.FileName,
                FullPath = pair.File.FilePath,
                FileSize =  pair.File.FileSize,
                VolumeMarker = pair.ParseResult.VolumeMarker,
                ChapterMarker = pair.ParseResult.ChapterMarker,
                Selected = toDownload.Contains(pair)
            }).ToList(),
        };

        uow.ExternalDownloadRepository.Add(externalDownload);
        await uow.CommitAsync(ct);

        externalDownload.Metadata.SetKey(RequestConstants.ExternalDownloadId, externalDownload.Id);
        uow.ExternalDownloadRepository.Update(externalDownload);
        await uow.CommitAsync(ct);

        return externalDownload;
    }

    internal async Task ApplyTorrentFileFiltersAsync(string requestId, List<ParsedTorrentFile> seriesFiles, List<ParsedTorrentFile> toDownload, bool newDownload, CancellationToken ct)
    {
        // Give qbit time to parse .torrent metadata
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var pathsToDownload = toDownload.Select(c => c.File.FilePath).ToList();

        await FilterContent(requestId, currentlySelected =>
        {
            if (newDownload) return pathsToDownload;

            var allSeriesPaths = seriesFiles.Select(p => p.File.FilePath);

            return currentlySelected
                .Except(allSeriesPaths)
                .Concat(pathsToDownload)
                .ToList();
        }, ct);
    }

    internal static async Task BroadcastDownloadStartedAsync(ResolvedServices services, DownloadRequestDto request, Series? series, ExternalDownload externalDownload)
    {
        var totalSize = externalDownload.TotalFileSize.AsHumanReadableSize();
        var toDownloadSize = externalDownload.SelectedFileSize.AsHumanReadableSize();

        var info = new DownloadInfo
        {
            Provider = request.Provider,
            Id = externalDownload.Id.ToString(),
            ContentState = ContentState.Queued,
            Name = externalDownload.Title,
            Description = series?.Summary,
            ImageUrl = series?.CoverUrl,
            RefUrl = series?.RefUrl,
            Size = toDownloadSize,
            ReDownloadSize = string.Empty,
            TotalSize = totalSize,
            Downloading = request.StartImmediately,
            Progress = 0,
            Estimated = 0,
            SpeedType = SpeedType.Bytes,
            Speed = 0,
            DownloadDir = Path.Join(externalDownload.BaseDir, externalDownload.Title),
            UserId = request.UserId,
            MonitoredSeriesId = request.GetKey(RequestConstants.MonitoredSeriesId),
        };

        await services.MessageService.AddContent(request.UserId, info);

        if (request.StartImmediately)
            services.ConnectionService.CommunicateDownloadStarted(info);
    }

    internal record ParsedTorrentFile(TorrentFile File, ParseResult ParseResult);

    #endregion
}
