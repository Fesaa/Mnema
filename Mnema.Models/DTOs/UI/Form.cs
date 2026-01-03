using System.Collections.Generic;
using Mnema.Common;

namespace Mnema.Models.DTOs.UI;

public sealed record FormDefinition
{
    public required string Key { get; set; }
    public string DescriptionKey { get; set; } = string.Empty;
    public required List<FormControlDefinition> Controls { get; set; }
}

public sealed record FormControlDefinition
{
    /// <summary>
    /// The translation key of the control, if <see cref="Field"/> is `metadata` also the key inside the <see cref="MetadataBag"/>
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The field on the value containing the value for this control. Defaults to metadata for historical reasons
    /// </summary>
    public string Field { get; set; } = "metadata";
    
    public GenericBag<object> Validators { get; set; } = new();

    public required FormType Type { get; set; }
    
    /// <summary>
    /// Only relevant if <see cref="Type"/> is <see cref="FormType.DropDown"/> or <see cref="FormType.MultiSelect"/>.
    /// Defaults to <see cref="UI.ValueType.String"/>
    /// </summary>
    /// <remarks>This must be <see cref="UI.ValueType.String"/> if <see cref="Field"/> is `metadata`</remarks>
    public ValueType ValueType { get; set; } = ValueType.String;
    public bool Advanced { get; set; } = false;
    public bool ForceSingle { get; set; } = false;
    public bool Disabled { get; set; } = false;
    
    public string DefaultOption { get; set; } = string.Empty;

    public List<FormControlOption> Options { get; set; } = [];
}

/// <summary>
/// 
/// </summary>
/// <param name="Key">Key to be used for translation</param>
/// <param name="Value">The value to be send back</param>
public sealed record FormControlOption(string Key, object Value)
{
    public bool Default { get; set; } = false;
    
    public static FormControlOption DefaultValue(string key, object value) => new(key, value)
    {
        Default = true
    };

    public static FormControlOption Option(string key, object value) => new(key, value)
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

public enum ValueType
{
    Boolean = 1,
    Integer = 2,
    String = 3,
}