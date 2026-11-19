using System;
using Mnema.Models.Enums;

namespace Mnema.Models.DTOs.Content;

public sealed record MonitoredSeriesLookupDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? CoverUrl { get; init; }
    public Provider Provider { get; init; }
}
