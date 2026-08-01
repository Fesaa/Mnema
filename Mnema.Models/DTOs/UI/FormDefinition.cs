using System.Collections.Generic;

namespace Mnema.Models.DTOs.UI;

public sealed record FormDefinition
{
    public required string Key { get; set; }
    public string DescriptionKey { get; set; } = string.Empty;
    public required List<FormFieldDefinition> Controls { get; set; }
}
