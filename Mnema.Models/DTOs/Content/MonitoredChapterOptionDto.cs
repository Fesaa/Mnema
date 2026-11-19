using System;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed class MonitoredChapterOptionDto
{
    public required Guid Id { get; init; }
    public required string Volume { get; init; }
    public required string Chapter { get; init; }
    public required string Title { get; init; }
    public required MonitoredChapterStatus Status { get; init; }
}
