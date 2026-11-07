using System;
using System.Collections.Generic;

namespace Mnema.Models.DTOs.Content;

public sealed record CombineChaptersDto
{
    public required List<Guid> ChapterIds { get; init; }
}
