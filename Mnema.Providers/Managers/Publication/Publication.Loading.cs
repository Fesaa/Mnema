using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.Publication;

namespace Mnema.Providers.Managers.Publication;

internal partial class Publication
{
    private readonly IScannerService _scannerService = scope.ServiceProvider.GetRequiredService<IScannerService>();
    private readonly IMetadataResolver _metadataResolver = scope.ServiceProvider.GetRequiredService<IMetadataResolver>();
    private readonly IParserService _parserService = scope.ServiceProvider.GetRequiredService<IParserService>();
    private MonitoredSeries? _monitoredSeries;
    private bool IsMonitored => _monitoredSeries != null;


    public async Task LoadMetadataAsync(CancellationTokenSource source)
    {
        _tokenSource = source;

        var cancellationToken = _tokenSource.Token;

        var sw = Stopwatch.StartNew();

        State = ContentState.Loading;
        await _messageService.StateUpdate(Id, ContentState.Loading);

        var monitoredSeriesId = Request.Metadata.GetKey(RequestConstants.MonitoredSeriesId);
        if (monitoredSeriesId != null)
        {
            _monitoredSeries = await _unitOfWork.MonitoredSeriesRepository.GetById(monitoredSeriesId.Value, ct: cancellationToken);
        }

        var preferences = await _unitOfWork.SettingsRepository.GetPreferencesAsync(cancellationToken);
        Preferences = preferences;

        try
        {
            await LoadSeriesInfo(cancellationToken);
        }
        catch (MnemaException e)
        {
            _logger.LogError(e, "[{Title}/{Id}] An error occured while loading series info", Title, Id);
            State = ContentState.Cancel;
            await Cancel(e);
            return;
        }

        FilterAlreadyDownloadedContent(cancellationToken);

        if (QueuedChapters.Count == 0 && Request.StartImmediately)
        {
            _logger.LogDebug("[{Title}/{Id}] No chapters to download, stopping download", Title, Id);
            State = ContentState.Cancel;
            await Cancel();
            return;
        }

        if (Request.StartImmediately && _monitoredSeries != null && !Request.GetKey(RequestConstants.FirstDownload) &&
            (QueuedChapters.Count > 10 || QueuedChapters.Count - ReDownloads == Series!.Chapters.Count))
        {
            _connectionService.CommunicateTooManyForAutomatedDownload(_monitoredSeries!, QueuedChapters.Count);
            Request.StartImmediately = false;
        }

        var allOneShots = Series!.Chapters.All(c => c.IsOneShot);
        if (allOneShots && !Request.GetKey(RequestConstants.DownloadOneShotKey) && Request.StartImmediately)
        {
            _connectionService.CommunicateDownloadInfo(DownloadInfo, "One-Shot Download",
                "All chapters in this series are One-Shots, but you've decided to not download one shots. Paused download, double check your decision!");
            Request.StartImmediately = false;
        }

        State = Request.StartImmediately ? ContentState.Ready : ContentState.Waiting;
        await _messageService.UpdateContent(DownloadInfo);

        _logger.LogDebug("[{Title}/{Id}] Loading metadata, {ToDownload}/{Total} chapters in {Elapsed}",
            Title, Id, QueuedChapters.Count, Series!.Chapters.Count, sw.Elapsed.ToReadableString());
    }

    internal void FilterAlreadyDownloadedContent(CancellationToken cancellationToken)
    {
        _logger.LogTrace("[{Title}/{Id}] Checking disk for content: {DownloadDir}", Title, Id, DownloadDir);

        var sw = Stopwatch.StartNew();

        // These default to Manga & Archive
        var contentFormat = Request.GetKey(RequestConstants.ContentFormatKey);
        var format = Request.GetKey(RequestConstants.FormatKey);

        ExistingContent =
            _scannerService.ScanDirectory(DownloadDir, contentFormat, format, cancellationToken);

        QueuedChapters = Series!.Chapters
            .Where(c => ShouldDownloadChapter(c, format))
            .Select(c => c.Id)
            .ToHashSet();

        if (sw.Elapsed.Seconds > 5)
            _logger.LogWarning("[{Title}/{Id}] Checking for existing content took a long time: {Elapsed}", Title, Id, sw.Elapsed.ToReadableString());
    }

    private bool ShouldDownloadChapter(Chapter chapter, Format format)
    {
        if (_monitoredSeries != null)
        {
            var monitoredChapter = _parserService.FindMatch(_monitoredSeries.Chapters, chapter);
            if (monitoredChapter?.Status == MonitoredChapterStatus.NotMonitored)
            {
                _logger.LogTrace("Ignoring chapter {ChapterId} as it is not monitored", chapter.Id);
                return false;
            }
        }

        var downloadOneShots = Request.GetKey(RequestConstants.DownloadOneShotKey);
        if (!downloadOneShots && string.IsNullOrEmpty(chapter.ChapterMarker))
        {
            _logger.LogTrace("Ignoring chapter {ChapterId} as it's not an oneshot", chapter.Id);
            return false;
        }

        #pragma warning disable CS0618
        // Chapter is present as a download (backwards compat with Media-Provider's old behavior)
        if (GetContentByFileName(VolumeDir(chapter) + format.FileExt()) != null)
        {
            _logger.LogTrace("Ignoring chapter {ChapterId} as it's already downloaded", chapter.Id);
            return false;
        }
        #pragma warning restore CS0618

        var content = GetContentByName(ChapterFileName(chapter));
        if (content == null)
        {
            content = GetContentByVolumeAndChapter(chapter.VolumeMarker, chapter.ChapterMarker);

            if (content == null)
            {
                // Some providers, *dynasty*, have terrible naming schemes for specials.
                if (Request.GetKey(RequestConstants.SkipVolumeWithoutChapter) &&
                    !string.IsNullOrEmpty(chapter.VolumeMarker)) return !string.IsNullOrEmpty(chapter.ChapterMarker);

                return true;
            }
        }

        var volumeChanged = !string.IsNullOrEmpty(chapter.VolumeMarker) && chapter.VolumeMarker != content.Volume;

        if (volumeChanged)
        {
            _logger.LogDebug("[{Title}/{Id}] Redownloading chapter {ChapterMarker} as volume changed from {Old} to {New}",
                Title, Id, chapter.ChapterMarker.I(), content.Volume.I(), chapter.VolumeMarker.I());
            ReDownloads++;
            ToRemovePaths.Add(content.Path);
        }

        return volumeChanged;
    }

    private async Task LoadSeriesInfo(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var metadata = Request.Metadata;
        metadata.SetKey(MetadataResolverOptions.MergeIntoUpstream, true);
        metadata.SetKey(MetadataResolverOptions.EnrichWithCovers, true);
        metadata.SetKey(RequestConstants.ExternalIdKey, Id);

        Series = await _metadataResolver.ResolveSeriesAsync(provider, metadata, cancellationToken);
        if (Series == null) throw new MnemaException($"No series found for id {Id}");

        if (string.IsNullOrWhiteSpace(Series.Title)) throw new MnemaException("No series title is set");

        _logger.LogDebug("[{Title}/{Id}] Successfully loaded series information with {Chapters} chapters in {Elapsed}",
            Title, Id, Series.Chapters.Count, sw.Elapsed.ToReadableString());
    }
}
