
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Providers;
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

public partial class Publication(IServiceScope scope, Provider provider)
{
    private ILogger<Publication> Logger { get; init; } = scope.ServiceProvider.GetRequiredService<ILogger<Publication>>();
    private IDownloadManager DownloadManager { get; init; } = scope.ServiceProvider.GetRequiredService<IDownloadManager>();
    private IRepository Repository { get; init; } = scope.ServiceProvider.GetRequiredKeyedService<IRepository>(provider);
    private DownloadRequestDto Request { get; init; } = scope.ServiceProvider.GetRequiredService<DownloadRequestDto>();
    
    public IPublicationExtensions Extensions { get; init; } = scope.ServiceProvider.GetRequiredService<IPublicationExtensions>();

    public CancellationTokenSource CancellationTokenSource { get; init; } = new ();

    public PublicationState State { get; private set; } = PublicationState.Queued;

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

    public string Id() => Series != null ? Series.Id : Request.Id;

    public string Title()
    {
        if (Series == null)
        {
            return Request.GetString(RequestConstants.TitleOverride).OrNonEmpty(Request.TempTitle, Request.Id);
        }
        
        return Request.GetString(RequestConstants.TitleOverride).OrNonEmpty(Series.Title, Request.Id);
    }

    public string DownloadDir => Series != null ? Path.Join(Request.BaseDir, Title()) : Request.BaseDir;

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
        Logger.LogTrace("Stopping download of {Id} - {Title}", Id(), Title());
        
        await CancellationTokenSource.CancelAsync();

        var req = new StopRequestDto
        {
            Provider = provider,
            Id = Id(),
            DeleteFiles = true,
        };

        try
        {
            await DownloadManager.StopDownload(req);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to remove download");
        }
    }

}