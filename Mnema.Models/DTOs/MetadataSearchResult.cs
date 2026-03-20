using System;
using Mnema.Models.Publication;

namespace Mnema.Models.DTOs;

public sealed record MetadataSearchResult: Series
{

    /// <summary>
    /// Present if the matched series is already monitored
    /// </summary>
    public Guid? MonitoredSeriesId { get; set; }

}
