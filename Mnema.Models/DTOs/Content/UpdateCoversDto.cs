using System;
using System.Collections.Generic;

namespace Mnema.Models.DTOs.Content;

public sealed record UpdateCoversDto
{
    public required List<Guid> ChapterIds { get; init; }
    public required string CoverUrl { get; init; }
}
