using System;
using System.Collections.Generic;
using System.Linq;
using Mnema.Common;

namespace Mnema.Models.DTOs.UI;

public static class FormFieldDefinitions
{
    public static DropDownFieldDefinition<TEnum> EnumMetadataDropDown<TEnum>(IMetadataKey<TEnum> key, string translationPrefix, bool forceEditMode = true)
        where TEnum : struct, Enum
    {
        return new DropDownFieldDefinition<TEnum>(FieldValueType.Integer)
        {
            Key = key.Key,
            ForceEditMode = forceEditMode,
            Options = Enum.GetValues<TEnum>()
                .Select(f => new SelectOption<TEnum>(f.ToString(), f)
                {
                    TranslationPrefix = translationPrefix
                })
                .ToList(),
        };
    }

    public static DropDownFieldDefinition<TEnum> EnumDropDown<TEnum>(string field, string translationPrefix, bool forceEditMode = true)
        where TEnum : struct, Enum
    {
        return EnumDropDown(field, translationPrefix, Enum.GetValues<TEnum>(), forceEditMode);
    }

    public static DropDownFieldDefinition<TEnum> EnumDropDown<TEnum>(string field, string translationPrefix, IEnumerable<TEnum> options, bool forceEditMode = true)
        where TEnum : struct, Enum
    {
        return new DropDownFieldDefinition<TEnum>(FieldValueType.Integer)
        {
            Field = field,
            ForceEditMode = forceEditMode,
            Options = options
                .Select(f => new SelectOption<TEnum>(f.ToString(), f)
                {
                    TranslationPrefix = translationPrefix
                })
                .ToList(),
        };
    }

    public static MultiSelectFieldDefinition<TEnum> EnumMultiSelect<TEnum>(string field, string translationPrefix,
        bool forceEditMode = true)
        where TEnum : struct, Enum
    {
        return EnumMultiSelect(field, translationPrefix, Enum.GetValues<TEnum>(), forceEditMode);
    }

    public static MultiSelectFieldDefinition<TEnum> EnumMultiSelect<TEnum>(string field, string translationPrefix,
        IEnumerable<TEnum> options, bool forceEditMode = true)
        where TEnum : struct, Enum
    {
        return new MultiSelectFieldDefinition<TEnum>(FieldValueType.Integer)
        {
            Field = field,
            ForceEditMode = forceEditMode,
            Options = options
                .Select(f => new SelectOption<TEnum>(f.ToString(), f)
                {
                    TranslationPrefix = translationPrefix
                })
                .ToList(),
        };
    }
}
