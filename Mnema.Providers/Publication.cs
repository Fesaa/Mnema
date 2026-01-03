using System.Diagnostics;
using System.IO.Abstractions;
using System.Threading.RateLimiting;
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
    ): IPublication
{
    public DownloadRequestDto Request { get; } = request;
    
    private readonly ILogger<Publication> _logger = scope.ServiceProvider.GetRequiredService<ILogger<Publication>>();
    private readonly IUnitOfWork _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    private readonly IPublicationManager _publicationManager = (IPublicationManager) scope.ServiceProvider.GetRequiredKeyedService<IContentManager>(provider);
    private readonly IRepository _repository = scope.ServiceProvider.GetRequiredKeyedService<IRepository>(provider);
    private readonly IPublicationExtensions _extensions = scope.ServiceProvider.GetRequiredKeyedService<IPublicationExtensions>(provider);
    private readonly IFileSystem _fileSystem = scope.ServiceProvider.GetRequiredService<IFileSystem>();
    private readonly ISettingsService _settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
    private readonly ApplicationConfiguration _configuration = scope.ServiceProvider.GetRequiredService<ApplicationConfiguration>();
    private readonly IMessageService _messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
    private readonly IMapper _mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
    private readonly IExternalConnectionService _externalConnectionService =  scope.ServiceProvider.GetRequiredService<IExternalConnectionService>();

    private CancellationTokenSource _tokenSource = new ();
    
    private Subscription? _subscription;
    private ServerSettingsDto _settings = null!;
    private FixedWindowRateLimiter _limiter = null!;

    internal UserPreferences Preferences = null!;
    internal Series? Series { get; private set; }

    private bool? _hasDuplicateVolumes  = null;

    /// <summary>
    /// List of <see cref="Chapter.Id"/> that are queued for downloading
    /// </summary>
    private IList<string> _queuedChapters = [];
    /// <summary>
    /// List of directory paths pointing to chapters we've downloaded this run 
    /// </summary>
    private IList<string> DownloadedPaths { get; } = [];
    /// <summary>
    /// List of paths pointing to chapters already on disk before this run
    /// </summary>
    private IList<OnDiskContent> ExistingContent { get; set; } = [];
    /// <summary>
    /// List of paths pointing to chapters that got replaced this run
    /// </summary>
    private IList<string> ToRemovePaths { get; set; } = [];

    public async Task Cleanup()
    {
        if (DownloadedPaths.Count == 0)
        {
            _logger.LogDebug("No newly downloaded items for {Id} - {Title}", Id, Title);
            return;
        }

        var sw = Stopwatch.StartNew();
        
        foreach (var path in ToRemovePaths)
        {
            _logger.LogTrace("Removing old chapter on {Path}", path);
            
            try
            {
                _fileSystem.File.Delete(_fileSystem.Path.Join(_configuration.BaseDir, path));
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to delete old file {File}", path);
            }
        }

        var baseDir = _fileSystem.Path.Join(_configuration.BaseDir, DownloadDir);
        if (!_fileSystem.Directory.Exists(baseDir))
        {
            _logger.LogDebug("Base directory {Dir} does not exist, creating", baseDir);
            _fileSystem.Directory.CreateDirectory(baseDir);
        }
        
        foreach (var path in DownloadedPaths)
        {
            _logger.LogTrace("Finalizing chapter {Path}", path);

            try
            {
                var src = _fileSystem.Path.Join(_configuration.DownloadDir, path);
                var dest = _fileSystem.Path.Join(_configuration.BaseDir, path);

                await _extensions.Cleanup(src, dest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occured finishing up a chapter at {Path}", path);
            }
        }
        
        _logger.LogDebug("Cleanup up {Id} - {Title} in {Elapsed}ms, removed {Deleted} old files, added {New} new files",
            Id, Title, sw.ElapsedMilliseconds, ToRemovePaths.Count, DownloadedPaths.Count);

        await CleanupNotifications();
    }

    private async Task CleanupNotifications()
    {
        _externalConnectionService.CommunicateDownloadFinished(DownloadInfo);
        
        if (!Request.IsSubscription)
            return;
        
        if (DownloadedPaths.Count == 0)
            return;

        var notification = new Notification
        {
            Title = "Download completed",
            UserId = Request.UserId,
            Summary =
                $"<a class=\"hover:pointer hover:underline\" href=\"%s\" target=\"_blank\">{DownloadInfo.RefUrl}</a> finished downloading {DownloadedPaths.Count} item(s). {_failedDownloadsTracker} failed on the first try.",
            Colour = NotificationColour.Primary,
        };
        
        _unitOfWork.NotificationRepository.AddNotification(notification);
        await _unitOfWork.CommitAsync();
        
        var dto = _mapper.Map<NotificationDto>(notification);
        await _messageService.Notify(Request.UserId, dto);
    }

    /// <summary>
    /// List of <see cref="Chapter.Id"/> selected by the user in the UI
    /// </summary>
    private List<string> _userSelectedIds = [];

    private int _failedDownloadsTracker = 0;
    private SpeedTracker? _speedTracker = null;

    public ContentState State { get; private set; } = ContentState.Queued;

    public DownloadInfo DownloadInfo => new()
    {
        Provider = provider,
        Id = Id,
        ContentState = State,
        Name = Title,
        RefUrl = Series?.RefUrl,
        Size = _userSelectedIds.Count > 0 ? $"{_userSelectedIds.Count} Chapters" : $"{_queuedChapters.Count} Chapters",
        Downloading = State == ContentState.Downloading,
        Progress = Math.Floor(_speedTracker?.Progress() ?? 0),
        Estimated = _speedTracker?.EstimatedTimeRemaining() ?? 0,
        SpeedType = SpeedType.Images,
        Speed = Math.Floor(_speedTracker?.IntermediateSpeed() ?? 0),
        DownloadDir = DownloadDir,
    };

    public string Id => Series != null ? Series.Id : Request.Id;

    public string Title =>  Series == null
        ? Request.GetString(RequestConstants.TitleOverride).OrNonEmpty(Request.TempTitle, Request.Id)
        : Request.GetString(RequestConstants.TitleOverride).OrNonEmpty(Series.Title, Request.Id);

    public string DownloadDir => Series != null ? Path.Join(Request.BaseDir, Title) : Request.BaseDir;

    private OnDiskContent? GetContentByName(string name) => ExistingContent.FirstOrDefault(c
        => c.Name == name);

    private OnDiskContent? GetContentByVolumeAndChapter(string volume, string chapter) => ExistingContent.FirstOrDefault(c =>
    {
        if (c.Volume == volume && c.Chapter == chapter) return true;

        // Content has been assigned a volume
        if (string.IsNullOrEmpty(c.Volume) && !string.IsNullOrEmpty(volume) && c.Chapter == chapter) return true;

        // Content has had its volume removed
        if (!string.IsNullOrEmpty(c.Volume) && string.IsNullOrEmpty(volume) && c.Chapter == chapter) return true;

        return false;
    });
    
    public async Task Cancel()
    {
        _logger.LogTrace("Stopping download of {Id} - {Title}", Id, Title);

        await _tokenSource.CancelAsync();

        if (await _publicationManager.GetPublicationById(Id) == null) return;

        try
        {
            await _publicationManager.StopDownload(StopRequest(true));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove download");
        }
    }

    private StopRequestDto StopRequest(bool deleteFiles) => new()
    {
        Provider = provider,
        Id = Id,
        DeleteFiles = deleteFiles,
        UserId = Request.UserId,
    };
}