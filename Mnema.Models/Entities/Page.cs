using System;
using Mnema.Common;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Enums;

namespace Mnema.Models.Entities;

public class Page: IDatabaseEntity
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string Icon { get; set; } = string.Empty;
    public required int SortValue { get; set; }
    public required Provider Provider { get; set; }
    public string CustomRootDir { get; set; } = string.Empty;
    public MetadataBag DefaultOptions { get; set; } = new();
}
