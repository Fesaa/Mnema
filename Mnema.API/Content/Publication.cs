using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;

namespace Mnema.API.Content;

public interface IPublicationManager : IContentManager
{
    Task<IPublication?> GetPublicationById(string id);
}

public interface IPublication : IContent
{
    Task Cancel();
    Task Cleanup();
    Task<MessageDto> ProcessMessage(MessageDto message);
    Task LoadMetadataAsync(CancellationTokenSource source);
    Task DownloadContentAsync(CancellationTokenSource source);
}

public class OnDiskContent
{
    public string SeriesName { get; init; }
    public string Path { get; init; }
    public string? Chapter { get; init; }
    public string? Volume { get; init; }
}

public static class RequestConstants
{
    public const string LanguageKey = "tl-lang";
    public const string AllowNonMatchingScanlationGroupKey = "allow_non_matching_scanlation_group";
    public const string DownloadOneShotKey = "download_one_shot";
    public const string IncludeNotMatchedTagsKey = "include_not_matched_tags";
    public const string IncludeCover = "include_cover";
    public const string UpdateCover = "update_cover";
    public const string TitleOverride = "title_override";
    public const string AssignEmptyVolumes = "assign_empty_volumes";
    public const string ScanlationGroupKey = "scanlation_group";
    public const string SkipVolumeWithoutChapter = "skip_volume_without_chapter";
    public const string FormatKey = "format";
    public const string ContentFormatKey = "content_format";
    public const string HardcoverSeriesIdKey = "hardcover_series_id";
    public const string MangaBakaKey = "manga_baka_id";
}
