using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Publication;

namespace Mnema.Providers;

internal partial class Publication 
{
    public async Task LoadMetadataAsync(CancellationTokenSource source)
    {
        _tokenSource = source;

        var cancellationToken = _tokenSource.Token;
        
        var sw = Stopwatch.StartNew();
        
        State = ContentState.Loading;
        
        var preferences = await _unitOfWork.UserRepository.GetPreferences(Request.UserId);
        if (preferences == null)
        {
            _logger.LogWarning("Failed to load user preferences, stopping downloading");
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
            _logger.LogError(e, "An error occured while loading series info");
            await Cancel();
            return;
        }

        if (Request.SubscriptionId != null)
        {
            _subscription = await _unitOfWork.SubscriptionRepository.GetSubscription(Request.SubscriptionId.Value);
            if (_subscription == null)
            {
                throw new MnemaException("Invalid subscription linked to download");
            }

            if (Preferences.PinSubscriptionTitles && !_subscription.Metadata.Extra.ContainsKey(RequestConstants.TitleOverride))
            {
                _subscription.Metadata.Extra.SetValue(RequestConstants.TitleOverride, Title);
                await _unitOfWork.CommitAsync();
            }
            
        }
        
        FilterAlreadyDownloadedContent(cancellationToken);

        if (_queuedChapters.Count == 0 && Request.DownloadMetadata.StartImmediately)
        {
            _logger.LogDebug("No chapters to download for {Title}, stopping download", Title);
            State = ContentState.Waiting;
            await _publicationManager.StopDownload(StopRequest(false));
            return;
        }

        State = Request.DownloadMetadata.StartImmediately ? ContentState.Ready : ContentState.Waiting;
        
        _logger.LogDebug("Loading metadata for {Title}, {ToDownload}/{Total} chapters in {Elapsed}ms",
            Title, _queuedChapters.Count, Series!.Chapters.Count, sw.ElapsedMilliseconds);
    }

    private void FilterAlreadyDownloadedContent(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking disk for content: {DownloadDir}", DownloadDir);

        var sw = Stopwatch.StartNew();
        
        ExistingContent = ParseDirectoryForContent(DownloadDir, cancellationToken);

        _queuedChapters = Series!.Chapters.Where(ShouldDownloadChapter).Select(c => c.Id).ToList();
        
        if (sw.Elapsed.Seconds > 5)
        {
            _logger.LogWarning("Checking for existing content took a long time: {Elapsed}s", sw.Elapsed.Seconds);
        }
    }

    private List<OnDiskContent> ParseDirectoryForContent(string path, CancellationToken cancellationToken)
    {
        var fullPath = Path.Join(_publicationManager.BaseDir, path);
        if (!_fileSystem.Directory.Exists(fullPath)) return [];


        var contents = new List<OnDiskContent>();
        
        foreach (var entry in _fileSystem.Directory.EnumerateFileSystemEntries(fullPath))
        {
            if (cancellationToken.IsCancellationRequested) return [];

            if (_fileSystem.Directory.Exists(entry))
            {
                contents.AddRange(ParseDirectoryForContent(entry, cancellationToken));
                continue;
            }

            var content = _extensions.ParseOnDiskFile(entry);
            if (content == null)
            {
                _logger.LogTrace("Ignoring {FileName} on disk", entry);
                continue;
            }
            
            _logger.LogTrace("Adding {FileName} to on disk content. (Vol. {Volume} Ch. {Chapter})", entry, content.Volume, content.Chapter);
            
            contents.Add(new OnDiskContent
            {
                Name = Path.GetFileNameWithoutExtension(entry),
                Path = entry,
                Volume = content.Volume,
                Chapter = content.Chapter,
            });
        }

        return contents;
    }

    private bool ShouldDownloadChapter(Chapter chapter)
    {
        // Chapter is present as a download
        if (GetContentByName(VolumeDir(chapter)) != null)
        {
            return false;
        }

        var content = GetContentByName(ChapterFileName(chapter));
        if (content == null)
        {
            content = GetContentByVolumeAndChapter(chapter.VolumeMarker, chapter.ChapterMarker);

            if (content == null)
            {
                // Some providers, *dynasty*, have terrible naming schemes for specials.
                if (Request.GetBool(RequestConstants.SkipVolumeWithoutChapter) && !string.IsNullOrEmpty(chapter.VolumeMarker))
                {
                    return !string.IsNullOrEmpty(chapter.ChapterMarker);
                }

                return true;
            }
        }

        var onDiskVolume = string.IsNullOrEmpty(content.Volume) ? _extensions.ParseVolumeFromFile(content) : content.Volume;
        if (onDiskVolume == null)
        {
            return false;
        }

        var volumeChanged = !string.IsNullOrEmpty(chapter.VolumeMarker) && chapter.VolumeMarker != onDiskVolume;

        if (volumeChanged)
        {
            _logger.LogDebug("Redownloading chapter {ChapterMarker} as volume changed from {Old} to {New}", 
                chapter.ChapterMarker, onDiskVolume, chapter.ChapterMarker);
            ToRemovePaths.Add(content.Path);
        }

        return volumeChanged;
    }

    private async Task LoadSeriesInfo(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        Series = await _repository.SeriesInfo(Request, cancellationToken);

        if (string.IsNullOrWhiteSpace(Series.Title))
        {
            throw new MnemaException("No series title is set");
        }

        _logger.LogDebug("Successfully loaded series information for {SeriesName} with {Chapters} chapters in {Elapsed}ms", Series.Title, Series.Chapters.Count, sw.ElapsedMilliseconds);

        if (!Request.GetBool(RequestConstants.AssignEmptyVolumes)) return;

        var hasAnyVolumes = Series.Chapters.Any(c => !string.IsNullOrEmpty(c.VolumeMarker));
        if (!hasAnyVolumes) return;

        foreach (var chapter in Series.Chapters.Where(c => string.IsNullOrEmpty(c.VolumeMarker) && !string.IsNullOrEmpty(c.ChapterMarker)))
        {
            chapter.VolumeMarker = "1";
        }
    }
}