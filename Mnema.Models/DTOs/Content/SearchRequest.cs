using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record SearchRequest
{
    public required Provider Provider { get; set; }
    public required string Query { get; set; }
    public required MetadataBag Modifiers { get; set; }
}