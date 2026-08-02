using System;
using System.Linq;

namespace Mnema.Models.DTOs.UI;

public static class FormFieldDefinitions
{
    public static DropDownFieldDefinition<TEnum> EnumDropDown<TEnum>(string field, string translationPrefix, bool forceEditMode = true)
        where TEnum : struct, Enum
    {
        return new DropDownFieldDefinition<TEnum>(FieldValueType.Integer)
        {
            Field = field,
            ForceEditMode = forceEditMode,
            Options = Enum.GetValues<TEnum>()
                .Select(f => new SelectOption<TEnum>(f.ToString(), f)
                {
                    TranslationPrefix = translationPrefix
                })
                .ToList(),
        };
    }
}
