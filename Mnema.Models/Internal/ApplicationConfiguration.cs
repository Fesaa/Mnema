using System.ComponentModel.DataAnnotations;

namespace Mnema.Models.Internal;

public sealed record ApplicationConfiguration
{
    [Required] public string BaseDir { get; init; } = "/";

    [Required] public string DownloadDir { get; init; }

    [Required] public string PersistentStorage { get; init; }
}
