using System.Collections.Generic;
using System.Text.Json.Serialization;
using Mnema.Common;

namespace Mnema.Models.DTOs.UI;

public abstract record FormFieldDefinition
{
    /// <summary>
    /// Translation key of the field. If Field is metadata, also the key inside MetadataBag.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Field on the value containing the value. Defaults to metadata for historical reasons.
    /// </summary>
    public string Field { get; init; } = "metadata";

    public GenericBag<object> Validators { get; init; } = new();

    public bool Advanced { get; init; }

    public bool Disabled { get; init; }

    public bool ForceSingle { get; init; }

    public abstract FormFieldType FieldType { get; }

    public abstract FieldValueType ValueType { get; }
}

public sealed record TextFieldDefinition : FormFieldDefinition
{
    public override FormFieldType FieldType => FormFieldType.Text;

    public override FieldValueType ValueType => FieldValueType.String;

    public string DefaultValue { get; init; } = string.Empty;
}

public sealed record MultiTextFieldDefinition : FormFieldDefinition
{
    public override FormFieldType FieldType => FormFieldType.MultiText;
    public override FieldValueType ValueType => FieldValueType.String;

    public List<SelectOption<string>> Options { get; init; } = [];
}

public sealed record DirectoryFieldDefinition : FormFieldDefinition
{
    public override FormFieldType FieldType => FormFieldType.Directory;
    public override FieldValueType ValueType => FieldValueType.String;
}

public sealed record IntegerFieldDefinition : FormFieldDefinition
{
    public override FormFieldType FieldType => FormFieldType.Text;

    public override FieldValueType ValueType => FieldValueType.Integer;

    public int? DefaultValue { get; init; }
}

public sealed record SwitchFieldDefinition : FormFieldDefinition
{
    public override FormFieldType FieldType => FormFieldType.Switch;

    public override FieldValueType ValueType => FieldValueType.Boolean;

    public bool DefaultValue { get; init; }
}

public abstract record SelectFieldDefinition<T> : FormFieldDefinition
{
    public List<SelectOption<T>> Options { get; init; } = [];

    public T? DefaultValue { get; init; }
}

public sealed record DropDownFieldDefinition<T>(FieldValueType FieldValueType = FieldValueType.String) : SelectFieldDefinition<T>
{
    public override FormFieldType FieldType => FormFieldType.DropDown;

    public override FieldValueType ValueType { get; } = FieldValueType;
}

public sealed record MultiSelectFieldDefinition<T>(FieldValueType FieldValueType = FieldValueType.String) : SelectFieldDefinition<T>
{
    public override FormFieldType FieldType => FormFieldType.MultiSelect;

    public override FieldValueType ValueType { get; } = FieldValueType;
}

public sealed record ArrayFieldDefinition : FormFieldDefinition
{
    public override FormFieldType FieldType => FormFieldType.Array;

    public override FieldValueType ValueType => FieldValueType.String;

    public List<FormFieldDefinition> Controls { get; init; } = [];
}
