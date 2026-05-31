using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Mnema.Common.Extensions;

public static class EnumExtensions
{
    public static string GetEnumMemberValue<T>(this T value) where T : Enum
    {
        return typeof(T)
                   .GetField(value.ToString())!
                   .GetCustomAttribute<EnumMemberAttribute>()?.Value
               ?? value.ToString();
    }

    public static T ParseEnumMemberValue<T>(this string value) where T : struct, Enum
    {
        foreach (var field in typeof(T).GetFields())
        {
            if (field.GetCustomAttribute<EnumMemberAttribute>()?.Value == value)
                return (T)field.GetValue(null)!;
        }
        return Enum.Parse<T>(value, ignoreCase: true);
    }
}
