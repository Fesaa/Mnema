using System;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public class MonitoredChapterDto
{
    public Guid Id { get; set; }
    public Guid SeriesId { get; set; }

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
