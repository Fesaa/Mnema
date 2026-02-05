using System;
using System.Collections.Generic;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.DTOs.UI;

public class PageDto: IDatabaseEntity
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string Icon { get; set; } = string.Empty;
    public required int SortValue { get; set; }
    public required Provider Provider { get; set; }
    public IList<FormControlDefinition>? Modifiers { get; set; }
    public string CustomRootDir { get; set; } = string.Empty;

    public List<FormControlDefinition> Metadata { get; set; } = [];
}
