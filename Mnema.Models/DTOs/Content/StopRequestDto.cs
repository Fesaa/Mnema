using System.Text.Json.Serialization;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record StopRequestDto
{
    public required Provider Provider { get; init; }
    public required string Id { get; init; }
    public required bool DeleteFiles { get; init; }
    
    [JsonIgnore]
    public required Guid UserId { get; set; }
}