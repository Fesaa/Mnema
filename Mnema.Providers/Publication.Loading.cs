using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Publication;

namespace Mnema.Providers;

internal partial class Publication
{
    private readonly IScannerService _scannerService = scope.ServiceProvider.GetRequiredService<IScannerService>();

    public async Task LoadMetadataAsync(CancellationTokenSource source)
    {
        _tokenSource = source;

        var cancellationToken = _tokenSource.Token;

        var sw = Stopwatch.StartNew();

        State = ContentState.Loading;
        await _messageService.StateUpdate(Request.UserId, Id, ContentState.Loading);

        var preferences = await _unitOfWork.UserRepository.GetPreferences(Request.UserId);
        if (preferences == null)
        {
            _logger.LogWarning("[{Title}/{Id}] Failed to load user preferences for {UserId}, stopping downloading", Title, Id, Request.UserId);
            State = ContentState.Cancel;
            await Cancel();
            return;
        }

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

        if (Request.SubscriptionId != null)
        {
            _subscription = await _unitOfWork.SubscriptionRepository
                .GetSubscription(Request.SubscriptionId.Value, cancellationToken);
            if (_subscription == null) throw new MnemaException("Invalid subscription linked to download");

            if (Preferences.PinSubscriptionTitles &&
                !_subscription.Metadata.ContainsKey(RequestConstants.TitleOverride))
            {
                _subscription.Metadata.SetValue(RequestConstants.TitleOverride, Title);
                await _unitOfWork.CommitAsync();
            }
        }

        FilterAlreadyDownloadedContent(cancellationToken);

        if (_queuedChapters.Count == 0 && (Request.StartImmediately || Request.IsSubscription))
        {
            _logger.LogDebug("[{Title}/{Id}] No chapters to download, stopping download", Title, Id);
            State = ContentState.Cancel;
            await Cancel();
            return;
        }

        State = Request.StartImmediately || Request.IsSubscription
            ? ContentState.Ready
            : ContentState.Waiting;
        await _messageService.UpdateContent(Request.UserId, DownloadInfo);

        _logger.LogDebug("[{Title}/{Id}] Loading metadata, {ToDownload}/{Total} chapters in {Elapsed}ms",
            Title, Id, _queuedChapters.Count, Series!.Chapters.Count, sw.ElapsedMilliseconds);
    }

    private void FilterAlreadyDownloadedContent(CancellationToken cancellationToken)
    {
        _logger.LogTrace("[{Title}/{Id}] Checking disk for content: {DownloadDir}", Title, Id, DownloadDir);

        var sw = Stopwatch.StartNew();

        ExistingContent =
            _scannerService.ScanDirectoryAsync(_extensions.ParseOnDiskFile, DownloadDir, cancellationToken);

        _queuedChapters = Series!.Chapters
            .Where(ShouldDownloadChapter)
            .Select(c => c.Id)
            .ToList();

        if (sw.Elapsed.Seconds > 5)
            _logger.LogWarning("[{Title}/{Id}] Checking for existing content took a long time: {Elapsed}s", Title, Id, sw.Elapsed.Seconds);
    }

    private bool ShouldDownloadChapter(Chapter chapter)
    {
        var downloadOneShots = Request.GetBool(RequestConstants.DownloadOneShotKey);
        if (!downloadOneShots && string.IsNullOrEmpty(chapter.ChapterMarker)) return false;

        // Chapter is present as a download
        if (GetContentByName(VolumeDir(chapter)) != null) return false;

        var content = GetContentByName(ChapterFileName(chapter));
        if (content == null)
        {
            content = GetContentByVolumeAndChapter(chapter.VolumeMarker, chapter.ChapterMarker);

            if (content == null)
            {
                // Some providers, *dynasty*, have terrible naming schemes for specials.
                if (Request.GetBool(RequestConstants.SkipVolumeWithoutChapter) &&
                    !string.IsNullOrEmpty(chapter.VolumeMarker)) return !string.IsNullOrEmpty(chapter.ChapterMarker);

                return true;
            }
        }

        var onDiskVolume = string.IsNullOrEmpty(content.Volume)
            ? _extensions.ParseVolumeFromFile(content)
            : content.Volume;
        if (onDiskVolume == null) return false;

        var volumeChanged = !string.IsNullOrEmpty(chapter.VolumeMarker) && chapter.VolumeMarker != onDiskVolume;

        if (volumeChanged)
        {
            _logger.LogDebug("[{Title}/{Id}] Redownloading chapter {ChapterMarker} as volume changed from {Old} to {New}",
                Title, Id, chapter.ChapterMarker, onDiskVolume, chapter.ChapterMarker);
            ToRemovePaths.Add(content.Path);
        }

        return volumeChanged;
    }

    private async Task LoadSeriesInfo(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        Series = await _repository.SeriesInfo(Request, cancellationToken);

        if (string.IsNullOrWhiteSpace(Series.Title)) throw new MnemaException("No series title is set");

        _logger.LogDebug(
            "[{Title}/{Id}] Successfully loaded series information with {Chapters} chapters in {Elapsed}ms",
            Title, Id, Series.Chapters.Count, sw.ElapsedMilliseconds);

        if (!Request.GetBool(RequestConstants.AssignEmptyVolumes)) return;

        var hasAnyVolumes = Series.Chapters.Any(c => !string.IsNullOrEmpty(c.VolumeMarker));
        if (!hasAnyVolumes) return;

        foreach (var chapter in Series.Chapters.Where(c =>
                     string.IsNullOrEmpty(c.VolumeMarker) && !string.IsNullOrEmpty(c.ChapterMarker)))
            chapter.VolumeMarker = "1";
    }
}
