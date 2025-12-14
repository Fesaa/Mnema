using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Publication;

namespace Mnema.Providers;

public partial class Publication 
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

        _preferences = preferences;
        
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

            if (_preferences.PinSubscriptionTitles && !_subscription.Metadata.Extra.ContainsKey(RequestConstants.TitleOverride))
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
            await Cancel();
            return;
        }

        State = Request.DownloadMetadata.StartImmediately ? ContentState.Ready : ContentState.Waiting;
        
        _logger.LogDebug("Loading metadata for {Title}, {ToDownload}/{Total} chapters in {Elapsed}ms",
            Title, _queuedChapters.Count, _series!.Chapters.Count, sw.ElapsedMilliseconds);
    }

    private void FilterAlreadyDownloadedContent(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking disk for content: {DownloadDir}", DownloadDir);

        var sw = Stopwatch.StartNew();
        
        _existingContent = ParseDirectoryForContent(DownloadDir, cancellationToken);

        _queuedChapters = _series!.Chapters.Where(ShouldDownloadChapter).Select(c => c.Id).ToList();
        
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
                contents.AddRange(ParseDirectoryForContent(Path.Join(path, entry), cancellationToken));
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
                Path = Path.Join(path, entry),
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

        var onDiskVolume = _extensions.ParseVolumeFromFile(this, content);
        if (onDiskVolume == null)
        {
            return false;
        }

        var volumeChanged = !string.IsNullOrEmpty(chapter.VolumeMarker) && chapter.VolumeMarker != onDiskVolume;

        if (volumeChanged)
        {
            _logger.LogDebug("Redownloading chapter {ChapterMarker} as volume changed from {Old} to {New}", 
                chapter.ChapterMarker, onDiskVolume, chapter.ChapterMarker);
            ToRemovePaths.Add(Path.Join(_publicationManager.BaseDir, content.Path));
        }

        return volumeChanged;
    }

    private async Task LoadSeriesInfo(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        _series = await _repository.SeriesInfo(Request, cancellationToken);

        if (string.IsNullOrWhiteSpace(_series.Title))
        {
            throw new MnemaException("No series title is set");
        }

        _logger.LogDebug("Successfully loaded series information for {SeriesName} with {Chapters} chapters in {Elapsed}ms", _series.Title, _series.Chapters.Count, sw.ElapsedMilliseconds);

        if (!Request.GetBool(RequestConstants.AssignEmptyVolumes)) return;

        var hasAnyVolumes = _series.Chapters.Any(c => !string.IsNullOrEmpty(c.VolumeMarker));
        if (!hasAnyVolumes) return;

        foreach (var chapter in _series.Chapters.Where(c => string.IsNullOrEmpty(c.VolumeMarker) && !string.IsNullOrEmpty(c.ChapterMarker)))
        {
            chapter.VolumeMarker = "1";
        }
    }
}