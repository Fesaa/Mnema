using System.ComponentModel.DataAnnotations;

namespace Mnema.Models.Internal;

public sealed record ApplicationConfiguration
{
    [Required]
    public string BaseDir { get; init; } 
    
}