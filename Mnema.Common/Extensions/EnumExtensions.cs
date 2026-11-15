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

    extension(string value)
    {
        public T ParseEnumMemberValue<T>() where T : struct, Enum
        {
            foreach (var field in typeof(T).GetFields())
            {
                if (field.GetCustomAttribute<EnumMemberAttribute>()?.Value == value)
                    return (T)field.GetValue(null)!;
            }
            return Enum.Parse<T>(value, ignoreCase: true);
        }

        public T ParseEnumMemberValueOrDefault<T>(T def = default) where T : struct, Enum
        {
            foreach (var field in typeof(T).GetFields())
            {
                if (field.GetCustomAttribute<EnumMemberAttribute>()?.Value == value)
                    return (T)field.GetValue(null)!;
            }
            return Enum.TryParse<T>(value, true, out var result) ? result : def;
        }
    }
}
