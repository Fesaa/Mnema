using System;
using Mnema.Common;
using Mnema.Models.Enums;

namespace Mnema.Models.DTOs.Content;

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
    public static readonly IMetadataKey<Format> FormatKey = MetadataKeys.Enum<Format>("format", Format.Archive);
    public static readonly IMetadataKey<ContentFormat> ContentFormatKey = MetadataKeys.Enum<ContentFormat>("contentFormat", ContentFormat.Manga);
    public static readonly IMetadataKey<string?> HardcoverSeriesIdKey = MetadataKeys.OptionalString("hardcover_series_id");
    public static readonly IMetadataKey<string?> MangaBakaKey = MetadataKeys.OptionalString("manga_baka_id");
    public static readonly IMetadataKey<string?> ExternalIdKey = MetadataKeys.OptionalString("external_id");
    public static readonly IMetadataKey<Guid?> MonitoredSeriesId = MetadataKeys.OptionalGuid("monitored_series_id");
    public static readonly IMetadataKey<bool> AllowPartialChapterData = MetadataKeys.Bool("allow_partial_chapter_data");
    public static readonly IMetadataKey<bool> FirstDownload = MetadataKeys.Bool("first_download");
    public static readonly IMetadataKey<bool> IgnoreNonMatchedVolumes = MetadataKeys.Bool("ignore_non_matched_volumes");
    public static readonly IMetadataKey<bool> IsGroupedDownload = MetadataKeys.Bool("is_grouped_download");
    public static readonly IMetadataKey<Guid?> ExternalDownloadId = MetadataKeys.OptionalGuid("external_download_id");
    public static readonly IMetadataKey<bool> AllowChapterDownloads = MetadataKeys.Bool("allow_chapter_downloads");
}
