using System;
using System.Collections.Generic;
using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record MonitoredSeriesDto
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    /// <inheritdoc cref="MonitoredSeries.Title" />
    public required string Title { get; init; }
    public string Summary { get; init; }
    public string? CoverUrl { get; init; }
    public string? RefUrl { get; init; }

    /// <inheritdoc cref="MonitoredSeries.Provider" />
    public List<Provider> Providers { get; init; }

    /// <inheritdoc cref="MonitoredSeries.BaseDir" />
    public required string BaseDir { get; init; }

    public ContentFormat ContentFormat { get; init; }
    public Format Format { get; init; }

    /// <inheritdoc cref="MonitoredSeries.ValidTitles" />
    public List<string> ValidTitles { get; init; }

    public string HardcoverId { get; init; }
    public string MangaBakaId { get; init; }
    /// <inheritdoc cref="MonitoredSeries.ExternalId" />
    public string ExternalId { get; set; }
    public string TitleOverride { get; init; }

    /// <inheritdoc cref="MonitoredSeries.Metadata" />
    public MetadataBag Metadata { get; set; }


    public DateTime CreatedUtc { get; init; }
    public DateTime LastModifiedUtc { get; init; }

    public DateTime LastDataRefreshUtc { get; init; }

    public List<MonitoredChapterDto> Chapters { get; init; }
}
