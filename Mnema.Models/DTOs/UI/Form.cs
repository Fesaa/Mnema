namespace Mnema.Models.DTOs.UI;

public sealed record FormControlDefinition
{
    public required string Key { get; set; }

    public required FormType Type { get; set; }

    public bool Advanced { get; set; } = false;
    
    public string DefaultOption { get; set; } = string.Empty;

    public List<FormControlOption> Options { get; set; } = [];
}

public sealed record FormControlOption(string Key, string Value)
{
    public bool Default { get; set; } = false;
    
    public static FormControlOption DefaultValue(string key, string value) => new FormControlOption(key, value)
    {
        Default = true
    };

    public static FormControlOption Option(string key, string value) => new FormControlOption(key, value)
    {
        Default = false
    };
};

public enum FormType
{
    Switch = 0,
    DropDown = 1,
    MultiSelect = 2,
    Text = 3,
}