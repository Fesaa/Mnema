using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.API.External;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;
using Mnema.Models.Internal;
using Mnema.Models.Publication;

namespace Mnema.Providers;

internal partial class Publication(
    IServiceScope scope,
    Provider provider,
    DownloadRequestDto request
) : IPublication
{
    private readonly ApplicationConfiguration _configuration =
        scope.ServiceProvider.GetRequiredService<ApplicationConfiguration>();

    private readonly IPublicationExtensions _extensions =
        scope.ServiceProvider.GetRequiredKeyedService<IPublicationExtensions>(provider);

    private readonly IExternalConnectionService _externalConnectionService =
        scope.ServiceProvider.GetRequiredService<IExternalConnectionService>();

    private readonly IFileSystem _fileSystem = scope.ServiceProvider.GetRequiredService<IFileSystem>();

    private readonly ILogger<Publication> _logger = scope.ServiceProvider.GetRequiredService<ILogger<Publication>>();
    private readonly IMapper _mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
    private readonly IMessageService _messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();

    private readonly IPublicationManager _publicationManager =
        (IPublicationManager)scope.ServiceProvider.GetRequiredKeyedService<IContentManager>(provider);

    private readonly IRepository _repository = scope.ServiceProvider.GetRequiredKeyedService<IRepository>(provider);
    private readonly ISettingsService _settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
    private readonly IUnitOfWork _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    private int _failedDownloadsTracker;

    private bool? _hasDuplicateVolumes;
    private FixedWindowRateLimiter _limiter = null!;

    /// <summary>
    ///     List of <see cref="Chapter.Id" /> that are queued for downloading
    /// </summary>
    private IList<string> _queuedChapters = [];

    private ServerSettingsDto _settings = null!;
    private SpeedTracker? _speedTracker;

    private Subscription? _subscription;

    private CancellationTokenSource _tokenSource = new();

    /// <summary>
    ///     List of <see cref="Chapter.Id" /> selected by the user in the UI
    /// </summary>
    private List<string> _userSelectedIds = [];

    internal UserPreferences Preferences = null!;
    internal Series? Series { get; private set; }

    /// <summary>
    ///     List of directory paths pointing to chapters we've downloaded this run
    /// </summary>
    private IList<string> DownloadedPaths { get; } = [];

    /// <summary>
    ///     List of paths pointing to chapters already on disk before this run
    /// </summary>
    private IList<OnDiskContent> ExistingContent { get; set; } = [];

    /// <summary>
    ///     List of paths pointing to chapters that got replaced this run
    /// </summary>
    private IList<string> ToRemovePaths { get; set; } = [];

    public DownloadRequestDto Request { get; } = request;

    public async Task Cleanup()
    {
        if (DownloadedPaths.Count == 0)
        {
            _logger.LogDebug("[{Title}/{Id}] No newly downloaded items", Title, Id);
            return;
        }

        var sw = Stopwatch.StartNew();

        foreach (var path in ToRemovePaths)
        {
            _logger.LogTrace("[{Title}/{Id}] Removing old chapter on {Path}", Title, Id, path);

            try
            {
                _fileSystem.File.Delete(path);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "[{Title}/{Id}] Failed to delete old file {File}", Title, Id, path);
            }
        }

        var baseDir = _fileSystem.Path.Join(_configuration.BaseDir, DownloadDir);
        if (!_fileSystem.Directory.Exists(baseDir))
        {
            _logger.LogDebug("[{Title}/{Id}] Base directory {Dir} does not exist, creating", Title, Id, baseDir);
            _fileSystem.Directory.CreateDirectory(baseDir);
        }

        foreach (var path in DownloadedPaths)
        {
            _logger.LogTrace("[{Title}/{Id}] Finalizing chapter {Path}", Title, Id, path);

            try
            {
                var src = _fileSystem.Path.Join(_configuration.DownloadDir, path);
                var dest = _fileSystem.Path.Join(_configuration.BaseDir, path);

                await _extensions.Cleanup(src, dest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Title}/{Id}] An exception occured finishing up a chapter at {Path}", Title, Id, path);
            }
        }

        _logger.LogDebug("[{Title}/{Id}] Cleanup finished in {Elapsed}ms, removed {Deleted} old files, added {New} new files",
            Title, Id, sw.ElapsedMilliseconds, ToRemovePaths.Count, DownloadedPaths.Count);

        await CleanupNotifications();
    }

    public ContentState State { get; private set; } = ContentState.Queued;

    public DownloadInfo DownloadInfo => new()
    {
        Provider = provider,
        Id = Id,
        ContentState = State,
        Name = Title,
        Description = Series?.Summary,
        ImageUrl = Series?.NonProxiedCoverUrl ?? Series?.CoverUrl,
        RefUrl = Series?.RefUrl,
        Size = _userSelectedIds.Count > 0 ? $"{_userSelectedIds.Count} Chapters" : $"{_queuedChapters.Count} Chapters",
        TotalSize = $"{Series?.Chapters.Count ?? 0} Chapters",
        Downloading = State == ContentState.Downloading,
        Progress = Math.Floor(_speedTracker?.Progress() ?? 0),
        Estimated = _speedTracker?.EstimatedTimeRemaining() ?? 0,
        SpeedType = SpeedType.Images,
        Speed = Math.Floor(_speedTracker?.IntermediateSpeed() ?? 0),
        DownloadDir = DownloadDir
    };

    public string Id => Series != null ? Series.Id : Request.Id;

    public string Title => Series == null
        ? Request.GetString(RequestConstants.TitleOverride).OrNonEmpty(Request.TempTitle, Request.Id)
        : Request.GetString(RequestConstants.TitleOverride).OrNonEmpty(Series.Title, Request.Id);

    public string DownloadDir => Series != null ? Path.Join(Request.BaseDir, Title) : Request.BaseDir;

    public Task Cancel() => Cancel(null);

    private async Task Cancel(Exception? reason = null)
    {
        _logger.LogTrace("[{Title}/{Id}] Stopping download", Title, Id);

        await _tokenSource.CancelAsync();

        if (reason != null)
        {
            _externalConnectionService.CommunicateDownloadFailure(DownloadInfo, reason);

        }

        if (await _publicationManager.GetPublicationById(Id) == null) return;

        try
        {
            await _publicationManager.StopDownload(StopRequest(true));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{Title}/{Id}] Failed to remove download", Title, Id);
        }
    }

    private async Task CleanupNotifications()
    {
        _externalConnectionService.CommunicateDownloadFinished(DownloadInfo);

        if (!Request.IsSubscription)
            return;

        if (DownloadedPaths.Count == 0)
            return;

        var info = DownloadInfo;

        var summary =
            $"<a class=\"hover:pointer hover:underline\" href=\"{info.RefUrl}\" target=\"_blank\">{Title}</a> finished downloading {DownloadedPaths.Count} item(s).";
        if (_failedDownloadsTracker > 0)
        {
            summary += $"{_failedDownloadsTracker} failed on the first try.";
        }

        var body = $"<bold>{Title}</bold><br>";
        foreach (var chapterId in _queuedChapters)
        {
            var chapter = Series!.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (chapter == null) continue;

            body += $"• {ChapterFileName(chapter)}\n";
        }

        var notification = new Notification
        {
            Title = "Download completed",
            UserId = Request.UserId,
            Summary = summary,
            Body = body,
            Colour = NotificationColour.Primary,
        };

        _unitOfWork.NotificationRepository.AddNotification(notification);
        await _unitOfWork.CommitAsync();

        await _messageService.NotificationAdded(Request.UserId, 1);

        var dto = _mapper.Map<NotificationDto>(notification);
        await _messageService.Notify(Request.UserId, dto);
    }

    private OnDiskContent? GetContentByName(string name)
    {
        return ExistingContent.FirstOrDefault(c
            => c.Name == name);
    }

    private OnDiskContent? GetContentByVolumeAndChapter(string volume, string chapter)
    {
        // OneShot can't be matched like this, needs to match on FileName
        if (string.IsNullOrEmpty(chapter)) return null;

        return ExistingContent.FirstOrDefault(c =>
        {
            if (c.Volume == volume && c.Chapter == chapter) return true;

            // Content has been assigned a volume
            if (string.IsNullOrEmpty(c.Volume) && !string.IsNullOrEmpty(volume) && c.Chapter == chapter) return true;

            // Content has had its volume removed
            if (!string.IsNullOrEmpty(c.Volume) && string.IsNullOrEmpty(volume) && c.Chapter == chapter) return true;

            return false;
        });
    }

    private StopRequestDto StopRequest(bool deleteFiles)
    {
        return new StopRequestDto
        {
            Provider = provider,
            Id = Id,
            DeleteFiles = deleteFiles,
            UserId = Request.UserId
        };
    }
}
