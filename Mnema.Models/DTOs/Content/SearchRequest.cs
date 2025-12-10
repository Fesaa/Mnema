using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record SearchRequest
{
    public required IList<Provider> Providers { get; set; }
    public required string Query { get; set; }
    public required MetadataBag Modifiers { get; set; }
}