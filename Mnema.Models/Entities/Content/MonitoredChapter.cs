using System;

namespace Mnema.Models.Entities.Content;

public class MonitoredChapter: IEntityDate
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; }

    public Guid SeriesId { get; set; }
    public MonitoredSeries Series { get; set; }

    public MonitoredChapterStatus Status { get; set; }

    public string Title { get; set; }
    public string Summary { get; set; }

    public string Volume { get; set; }
    public string Chapter { get; set; }

    public string? CoverUrl { get; set; }
    public string? RefUrl { get; set; }

    public string? FilePath { get; set; }

    /// <summary>
    /// Chapters without a release date are considered available
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

public enum MonitoredChapterStatus
{
    /// <summary>
    /// This chapter should be ignored
    /// </summary>
    NotMonitored = 0,
    /// <summary>
    /// This chapter is available, wanted, but not on disk
    /// </summary>
    Missing = 1,
    /// <summary>
    /// This is chapter is not yet available, but known
    /// </summary>
    Upcoming = 2,
    /// <summary>
    /// This chapter is currently being imported/downloaded
    /// </summary>
    Importing = 3,
    /// <summary>
    /// The chapter is found on disk
    /// </summary>
    Available = 4,

}
