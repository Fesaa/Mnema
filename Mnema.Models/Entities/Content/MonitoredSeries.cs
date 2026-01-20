using System;
using System.Collections.Generic;
using Mnema.Common;

namespace Mnema.Models.Entities.Content;

public class MonitoredSeries: IEntityDate
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// This title has no effect on actual downloads
    /// </summary>
    public required string Title { get; set; }
    public string Summary { get; set; }
    public string? CoverUrl { get; set; }
    public string? RefUrl { get; set; }

    /// <summary>
    /// Providers this release may be found on
    /// </summary>
    public List<Provider> Providers { get; set; }

    /// <summary>
    ///     The directory to download the content in
    /// </summary>
    public required string BaseDir { get; set; }

    public ContentFormat ContentFormat { get; set; }
    public Format Format { get; set; }

    /// <summary>
    /// Titles that are considered valid for this release. I.e., Translated, original, romanized, etc.
    /// </summary>
    /// <remarks>You can use the auto-complete in the UI to load from metadata providers</remarks>
    public List<string> ValidTitles { get; set; }

    public string HardcoverId { get; set; }
    public string MangaBakaId { get; set; }
    /// <summary>
    /// This is the ID from a <see cref="Provider"/>. This only makes sense for Mangadex, etc. Not for torrent sites
    /// </summary>
    public string ExternalId { get; set; }
    public string TitleOverride { get; set; }

    /// <summary>
    /// This holds metadata for the download option from the <see cref="Providers"/>. Some options will be filtered out
    /// as they're stored in the object itself.
    /// </summary>
    public MetadataBag Metadata { get; set; }


    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    public DateTime LastDataRefreshUtc { get; set; }

    public List<MonitoredChapter> Chapters { get; set; }
}
