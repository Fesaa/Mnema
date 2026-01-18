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
    ///     The translation key of the control, if <see cref="Field" /> is `metadata` also the key inside the
    ///     <see cref="MetadataBag" />
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    ///     The field on the value containing the value for this control. Defaults to metadata for historical reasons
    /// </summary>
    public string Field { get; set; } = "metadata";

    public GenericBag<object> Validators { get; set; } = new();

    public required FormType Type { get; set; }

    /// <summary>
    ///     Only relevant if <see cref="Type" /> is <see cref="FormType.DropDown" />, <see cref="FormType.MultiSelect" /> or <see cref="FormType.MultiText" />.
    ///     Defaults to <see cref="FormValueType.String" />
    /// </summary>
    /// <remarks>This must be <see cref="FormValueType.String" /> if <see cref="Field" /> is `metadata`</remarks>
    public FormValueType ValueType { get; set; } = FormValueType.String;

    public bool Advanced { get; set; } = false;
    public bool ForceSingle { get; set; } = false;
    public bool Disabled { get; set; } = false;

    public object DefaultOption { get; set; } = string.Empty;

    public List<FormControlOption> Options { get; set; } = [];
}

/// <summary>
/// </summary>
/// <param name="Key">Key to be used for translation</param>
/// <param name="Value">The value to be sent back</param>
public sealed record FormControlOption(string Key, object Value)
{
    public bool Default { get; set; }

    public FormControlOption(string v) : this(v, v) {}

    public static FormControlOption DefaultValue(string key, object value)
    {
        return new FormControlOption(key, value)
        {
            Default = true
        };
    }

    public static FormControlOption Option(string key, object value)
    {
        return new FormControlOption(key, value)
        {
            Default = false
        };
    }
}

public enum FormType
{
    Switch = 0,
    DropDown = 1,
    MultiSelect = 2,
    Text = 3,
    Directory = 4,
    MultiText = 5
}

public enum FormValueType
{
    Boolean = 1,
    Integer = 2,
    String = 3
}

public sealed class FormValidatorsBuilder : Builder<GenericBag<object>>
{
    private readonly GenericBag<object> _validators = new();

    public FormValidatorsBuilder WithMinLength(int minLength)
    {
        _validators.SetValue("minLength", minLength);
        return this;
    }

    public FormValidatorsBuilder WithMaxLength(int maxLength)
    {
        _validators.SetValue("maxLength", maxLength);
        return this;
    }

    public FormValidatorsBuilder WithRequired()
    {
        _validators.SetValue("required");
        return this;
    }

    public FormValidatorsBuilder WithMin(int min)
    {
        _validators.SetValue("min", min);
        return this;
    }

    public FormValidatorsBuilder WithMax(int max)
    {
        _validators.SetValue("max", max);
        return this;
    }

    public FormValidatorsBuilder WithPattern(string pattern)
    {
        _validators.SetValue("pattern", pattern);
        return this;
    }

    public FormValidatorsBuilder WithStartsWith(string prefix)
    {
        _validators.SetValue("startsWith", prefix);
        return this;
    }

    public FormValidatorsBuilder WithIsUrl()
    {
        _validators.SetValue("isUrl");
        return this;
    }

    public override GenericBag<object> Build()
    {
        return _validators;
    }
}
