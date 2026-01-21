using System;
using System.Collections.Generic;
using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public class CreateOrUpdateMonitoredSeriesDto
{
    public Guid Id { get; set; } = Guid.Empty;

    public required string Title { get; set; }

    public List<string> ValidTitles { get; set; } = [];

    public Provider Provider { get; set; }

    public required string BaseDir { get; set; }

    public ContentFormat ContentFormat { get; set; }
    public Format Format { get; set; }

    public string HardcoverId { get; set; }
    public string MangaBakaId { get; set; }
    /// <inheritdoc cref="MonitoredSeries.ExternalId" />
    public string ExternalId { get; set; }
    public string TitleOverride { get; set; }
    /// <inheritdoc cref="MonitoredSeries.Metadata" />
    public MetadataBag Metadata { get; set; }

}
