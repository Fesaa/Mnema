using System;
using System.Collections.Generic;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.UI;

public class PageDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string Icon { get; set; } = string.Empty;
    public required int SortValue { get; set; }
    public required Provider Provider { get; set; }
    public IList<FormControlDefinition>? Modifiers { get; set; }
    public string CustomRootDir { get; set; } = string.Empty;
    
    public DownloadMetadata? Metadata { get; set; }

}

public sealed record DownloadMetadata(List<FormControlDefinition> Definitions);