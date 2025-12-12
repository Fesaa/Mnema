
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
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

internal static class RequestConstants
{
    public const string LanguageKey                        = "tl-lang";
    public const string AllowNonMatchingScanlationGroupKey = "allow_non_matching_scanlation_group";
    public const string DownloadOneShotKey                 = "download_one_shot";
    public const string IncludeNotMatchedTagsKey           = "include_not_matched_tags";
    public const string IncludeCover                       = "include_cover";
    public const string UpdateCover                        = "update_cover";
    public const string TitleOverride                      = "title_override";
    public const string AssignEmptyVolumes                 = "assign_empty_volumes";
    public const string ScanlationGroupKey                 = "scanlation_group";
    public const string SkipVolumeWithoutChapter           = "skip_volume_without_chapter";
}

public interface IPublicationExtensions
{
    Task DownloadCallback();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    OnDiskContent? ParseOnDiskFile(string fileName);
    /// <summary>
    /// Called during cleanup
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    Task Cleanup(string path);
}

public partial class Publication(IServiceScope scope, Provider provider, DownloadRequestDto request): IPublication
{
    private ILogger<Publication> Logger { get; } = scope.ServiceProvider.GetRequiredService<ILogger<Publication>>();
    private IUnitOfWork UnitOfWork { get; } = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    private IPublicationManager PublicationManager { get; } = (IPublicationManager) scope.ServiceProvider.GetRequiredKeyedService<IContentManager>(provider.ToString());
    private IRepository Repository { get; } = scope.ServiceProvider.GetRequiredKeyedService<IRepository>(provider.ToString());
    public IPublicationExtensions Extensions { get; } = scope.ServiceProvider.GetRequiredKeyedService<IPublicationExtensions>(provider.ToString());
    private DownloadRequestDto Request { get; } = request;

    public ContentState State { get; private set; } = ContentState.Queued;

    private UserPreferences Preferences { get; set; } = null!;
    private Series? Series { get; set; } = null;

    private TriState HasDuplicateVolumes { get; set; } = TriState.NotSet;

    /// <summary>
    /// List of <see cref="Chapter.Id"/> that are queued for downloading
    /// </summary>
    private IList<string> QueuedChapters { get; set; } = [];
    /// <summary>
    /// List of directory paths pointing to chapters we've downloaded this run 
    /// </summary>
    public IList<string> DownloadedPaths { get; set; } = [];
    /// <summary>
    /// List of paths pointing to chapters already on disk before this run
    /// </summary>
    public IList<OnDiskContent> ExistingContent { get; set; } = [];
    /// <summary>
    /// List of paths pointing to chapters that got replaced this run
    /// </summary>
    public IList<string> ToRemovePaths { get; set; } = [];
    /// <summary>
    /// List of <see cref="Chapter.Id"/> selected by the user in the UI
    /// </summary>
    public IList<string> UserSelectedIds { get; set; } = [];

    private int FailedDownloadsTracker { get; set; } = 0;
    private SpeedTracker? SpeedTracker { get; set; } = null;

    public string Id => Series != null ? Series.Id : Request.Id;

    public string Title =>  Series == null
        ? Request.GetString(RequestConstants.TitleOverride).OrNonEmpty(Request.TempTitle, Request.Id) 
        : Request.GetString(RequestConstants.TitleOverride).OrNonEmpty(Series.Title, Request.Id);

    public string DownloadDir => Series != null ? Path.Join(Request.BaseDir, Title) : Request.BaseDir;

    public OnDiskContent? GetContentByName(string name) => ExistingContent.FirstOrDefault(c
        => Path.GetFileNameWithoutExtension(c.Name) == name);

    public OnDiskContent? GetContentByVolumeAndChapter(string volume, string chapter) => ExistingContent.FirstOrDefault(c =>
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
        Logger.LogTrace("Stopping download of {Id} - {Title}", Id, Title);

        var req = new StopRequestDto
        {
            Provider = provider,
            Id = Id,
            DeleteFiles = true,
        };

        try
        {
            await PublicationManager.StopDownload(req);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to remove download");
        }
    }

    public async Task LoadMetadataAsync(CancellationToken cancellationToken)
    {
        State = ContentState.Loading;

        var sw = Stopwatch.StartNew();

        var preferences = await UnitOfWork.UserRepository.GetPreferences(Request.UserId);
        if (preferences == null)
        {
            Logger.LogWarning("Failed to load user preferences, stopping downloading");
            await Cancel();
            return;
        }

        Preferences = preferences;
    }

    public Task DownloadContentAsync(CancellationToken cancellation)
    {
        throw new NotImplementedException();
    }
}