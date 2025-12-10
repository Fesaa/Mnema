namespace Mnema.Models.DTOs.UI;

public enum ModifierType
{
    DropDown = 1,
    Multi = 2,
    Switch = 3,
}

public sealed record ModifierDto
{
    public required string Title { get; set; }
    public required ModifierType Type { get; set; }
    public required string Key { get; set; }    
    public required IList<ModifierValueDto> Values { get; set; }
    public required int Sort { get; set; }
}

public sealed record ModifierValueDto
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public bool Default { get; set; } = false;
}