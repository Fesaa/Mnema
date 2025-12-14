using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;
using Mnema.Models.Publication;

namespace Mnema.Providers;

public class OnDiskContent
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public string? Chapter { get; init; }
    public string? Volume { get; init; }
}

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

    private CancellationTokenSource _tokenSource = new ();
    
    private Subscription? _subscription;
    private UserPreferences _preferences = null!;
    private Series? _series;

    private bool? _hasDuplicateVolumes  = null;

    /// <summary>
    /// List of <see cref="Chapter.Id"/> that are queued for downloading
    /// </summary>
    private IList<string> _queuedChapters = [];
    /// <summary>
    /// List of directory paths pointing to chapters we've downloaded this run 
    /// </summary>
    public IList<string> DownloadedPaths = [];
    /// <summary>
    /// List of paths pointing to chapters already on disk before this run
    /// </summary>
    public IList<OnDiskContent> _existingContent = [];
    /// <summary>
    /// List of paths pointing to chapters that got replaced this run
    /// </summary>
    public IList<string> ToRemovePaths = [];
    /// <summary>
    /// List of <see cref="Chapter.Id"/> selected by the user in the UI
    /// </summary>
    private IList<string> _userSelectedIds = [];

    private int _failedDownloadsTracker = 0;
    private SpeedTracker? _speedTracker = null;

    public ContentState State { get; private set; } = ContentState.Queued;

    public DownloadInfo DownloadInfo => new()
    {
        Provider = provider,
        Id = Id,
        ContentState = State,
        Name = Title,
        RefUrl = _series?.RefUrl,
        Size = _userSelectedIds.Count > 0 ? $"{_userSelectedIds.Count} Chapters" : $"{_queuedChapters.Count} Chapters",
        Downloading = State == ContentState.Downloading,
        Progress = Math.Floor(_speedTracker?.Progress() ?? 0),
        Estimated = 0,
        SpeedType = SpeedType.Images,
        Speed = Math.Floor(_speedTracker?.Speed() ?? 0),
        DownloadDir = DownloadDir,
    };

    public string Id => _series != null ? _series.Id : Request.Id;

    public string Title =>  _series == null
        ? Request.GetString(RequestConstants.TitleOverride).OrNonEmpty(Request.TempTitle, Request.Id)
        : Request.GetString(RequestConstants.TitleOverride).OrNonEmpty(_series.Title, Request.Id);

    private string DownloadDir => _series != null ? Path.Join(Request.BaseDir, Title) : Request.BaseDir;

    private OnDiskContent? GetContentByName(string name) => _existingContent.FirstOrDefault(c
        => Path.GetFileNameWithoutExtension(c.Name) == name);

    private OnDiskContent? GetContentByVolumeAndChapter(string volume, string chapter) => _existingContent.FirstOrDefault(c =>
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

        var req = new StopRequestDto
        {
            Provider = provider,
            Id = Id,
            DeleteFiles = true,
            UserId = Request.UserId,
        };

        try
        {
            await _publicationManager.StopDownload(req);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove download");
        }
    }
}