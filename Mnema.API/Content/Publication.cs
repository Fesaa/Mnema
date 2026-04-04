using System;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

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
    public string SeriesName { get; set; }
    public string Path { get; set; }
    public string FileName { get; set; }
    public string? Chapter { get; set; }
    public string? Volume { get; set; }
}

public static class RequestConstants
{
    public static readonly IMetadataKey<string> LanguageKey = MetadataKeys.String("tl-lang", "en");
    public static readonly IMetadataKey<bool> AllowNonMatchingScanlationGroupKey = MetadataKeys.Bool("allow_non_matching_scanlation_group", true);
    public static readonly IMetadataKey<bool> DownloadOneShotKey = MetadataKeys.Bool("download_one_shot");
    public static readonly IMetadataKey<bool> IncludeNotMatchedTagsKey = MetadataKeys.Bool("include_not_matched_tags");
    public static readonly IMetadataKey<bool> IncludeCover = MetadataKeys.Bool("include_cover", true);
    public static readonly IMetadataKey<string?> TitleOverride = MetadataKeys.OptionalString("title_override");
    public static readonly IMetadataKey<string> ScanlationGroupKey = MetadataKeys.String("scanlation_group", string.Empty);
    public static readonly IMetadataKey<bool> SkipVolumeWithoutChapter = MetadataKeys.Bool("skip_volume_without_chapter");
    public static readonly IMetadataKey<Format> FormatKey = MetadataKeys.Enum<Format>("format");
    public static readonly IMetadataKey<ContentFormat> ContentFormatKey = MetadataKeys.Enum<ContentFormat>("content_format");
    public static readonly IMetadataKey<string?> HardcoverSeriesIdKey = MetadataKeys.OptionalString("hardcover_series_id");
    public static readonly IMetadataKey<string?> MangaBakaKey = MetadataKeys.OptionalString("manga_baka_id");
    public static readonly IMetadataKey<string?> ExternalIdKey = MetadataKeys.OptionalString("external_id");
    public static readonly IMetadataKey<Guid?> MonitoredSeriesId = MetadataKeys.OptionalGuid("monitored_series_id");
    public static readonly IMetadataKey<bool> AllowPartialChapterData = MetadataKeys.Bool("allow_partial_chapter_data");
    public static readonly IMetadataKey<bool> FirstDownload = MetadataKeys.Bool("first_download");
    public static readonly IMetadataKey<bool> IgnoreNonMatchedVolumes = MetadataKeys.Bool("ignore_non_matched_volumes");
}
