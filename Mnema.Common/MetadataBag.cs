using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Mnema.Common;

public class MetadataBag : GenericBag<string>
{
    public IEnumerable<string> GetStrings(string key)
    {
        return TryGetValue(key, out var list) ? list : [];
    }

    public string? GetString(string key, string? fallback = null)
    {
        if (TryGetValue(key, out var list) && list.Count > 0) return list[0];

        return string.IsNullOrEmpty(fallback) ? null : fallback;
    }

    public TEnum? GetEnum<TEnum>(string key) where TEnum : struct
    {
        var value = GetString(key);
        if (string.IsNullOrEmpty(value))
            return null;

        return Enum.TryParse<TEnum>(value, true, out var result) ? result : default;
    }

    [return:NotNullIfNotNull(nameof(fallback))] public string? GetStringOrDefault(string key, string? fallback)
    {
        var value = GetString(key);

        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    public bool GetBool(string key, bool fallback = false)
    {
        var value = GetString(key);

        return string.IsNullOrEmpty(value)
            ? fallback
            : value.Equals("true", StringComparison.InvariantCultureIgnoreCase);
    }
}

public class GenericBag<T> : Dictionary<string, IList<T>>
{
    public void SetValue(string key, params T[] value)
    {
        TryAdd(key, value.ToList());
    }
}
