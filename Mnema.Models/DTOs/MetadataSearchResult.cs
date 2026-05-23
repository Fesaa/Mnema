using System;
using System.Collections.Generic;
using Mnema.Models.Publication;

namespace Mnema.Models.DTOs;

public sealed record MetadataSearchResult: Series
{

    /// <summary>
    /// Present if the matched series is already monitored
    /// </summary>
    public List<Guid> MonitoredSeriesId { get; set; } = [];

}
