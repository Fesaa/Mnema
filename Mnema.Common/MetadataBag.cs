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

    internal string? GetString(string key, string? fallback = null)
    {
        if (TryGetValue(key, out var list) && list.Count > 0) return list[0];

        return string.IsNullOrEmpty(fallback) ? null : fallback;
    }

    internal TEnum GetRequiredEnum<TEnum>(string key) where TEnum : struct, Enum
    {
        var value = GetEnum<TEnum>(key);
        return value ?? throw new ArgumentException($"Required enum value '{key}' not found.");
    }

    internal TEnum? GetEnum<TEnum>(string key) where TEnum : struct
    {
        var value = GetString(key);
        if (string.IsNullOrEmpty(value))
            return null;

        return Enum.TryParse<TEnum>(value, true, out var result) ? result : default;
    }

    [return:NotNullIfNotNull(nameof(fallback))]
    public string? GetStringOrDefault(string key, string? fallback)
    {
        var value = GetString(key);

        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    internal bool GetBool(string key, bool fallback = false)
    {
        var value = GetString(key);

        return string.IsNullOrEmpty(value)
            ? fallback
            : value.Equals("true", StringComparison.InvariantCultureIgnoreCase);
    }

    internal Guid? GetGuid(string key)
    {
        var value = GetString(key);
        if (string.IsNullOrEmpty(value)) return null;

        return Guid.TryParse(value, out var result) ? result : null;
    }

    internal int? GetInt(string key)
    {
        var value = GetString(key);
        if (string.IsNullOrEmpty(value)) return null;

        return int.TryParse(value, out var result) ? result : null;
    }

    internal void SetBool(string key, bool b)
    {
        SetValue(key, b ? "true" : "false");
    }

    internal void SetInt(string key, int i)
    {
        SetValue(key, i.ToString());
    }

    internal void SetEnum<TEnum>(string key, TEnum value) where TEnum : struct, Enum
    {
        SetValue(key, value.ToString());
    }

    internal void SetGuid(string key, Guid guid)
    {
        SetValue(key, guid.ToString());
    }

    public T GetKey<T>(IMetadataKey<T> key)
    {
        return key.Get(this);
    }

    public void SetKey<T>(IMetadataKey<T> key, T value)
    {
        key.Set(this, value);
    }
}

public class GenericBag<T> : Dictionary<string, IList<T>>
{
    public void SetValue(string key, params T[] value)
    {
        this[key] = value.ToList();
    }
}

public interface IMetadataKey<T>
{

    string Key { get; }

    T Get(MetadataBag bag);
    void Set(MetadataBag bag, T value);
}

public static class MetadataKeys
{
    public static IMetadataKey<string> String(string key, string fallback = "")
    {
        return new MetadataKey<string>(key, m =>
        {
            var value = m.GetString(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }, (m, value) => m.SetValue(key, value));
    }

    public static IMetadataKey<string?> OptionalString(string key, string? fallback = null)
    {
        return new MetadataKey<string?>(key,
            m => m.GetStringOrDefault(key, fallback),
            (m, value) =>
            {
                if (!string.IsNullOrEmpty(value))
                    m.SetValue(key, value);
                else
                    m.Remove(key);
            });
    }

    public static IMetadataKey<bool> Bool(string key, bool fallback = false)
    {
        return new MetadataKey<bool>(key,
            m => m.GetBool(key, fallback),
            (m, value) => m.SetBool(key, value));
    }

    public static IMetadataKey<int> Int(string key, int? fallback = null)
    {
        return new MetadataKey<int>(key,
            m =>
            {
                var value = m.GetInt(key);
                if (value.HasValue) return value.Value;

                if (fallback.HasValue) return fallback.Value;

                throw new ArgumentException($"Required int value '{key}' not found.");
            },
            (m, value) => m.SetInt(key, value));
    }

    public static IMetadataKey<int?> OptionalInt(string key)
    {
        return new MetadataKey<int?>(key,
            m => m.GetInt(key),
            (m, value) =>
            {
                if (value.HasValue)
                    m.SetInt(key, value.Value);
                else
                    m.Remove(key);
            });
    }

    public static IMetadataKey<TEnum> Enum<TEnum>(string key) where TEnum : struct, Enum
    {
        return new MetadataKey<TEnum>(key,
            m => m.GetRequiredEnum<TEnum>(key),
            (m, value) => m.SetEnum(key, value));
    }

    public static IMetadataKey<TEnum?> OptionalEnum<TEnum>(string key) where TEnum : struct, Enum
    {
        return new MetadataKey<TEnum?>(key,
            m => m.GetEnum<TEnum>(key),
            (m, value) =>
            {
                if (value.HasValue)
                    m.SetEnum(key, value.Value);
                else
                    m.Remove(key);
            });
    }

    public static IMetadataKey<Guid?> OptionalGuid(string key)
    {
        return new MetadataKey<Guid?>(key,
            m => m.GetGuid(key),
            (m, value) =>
            {
                if (value.HasValue)
                    m.SetGuid(key, value.Value);
                else
                    m.Remove(key);
            });
    }

    public static IMetadataKey<IEnumerable<string>> Strings(string key)
    {
        return new MetadataKey<IEnumerable<string>>(key, m => m.GetStrings(key), (m, value) => m.SetValue(key, value.ToArray()));
    }
}

internal sealed class MetadataKey<T>: IMetadataKey<T>
{
    public string Key { get; init; }
    private readonly Func<MetadataBag, T> _getter;
    private readonly Action<MetadataBag, T> _setter;

    internal MetadataKey(string key, Func<MetadataBag, T> getter, Action<MetadataBag, T> setter)
    {
        Key = key;
        _getter = getter;
        _setter = setter;
    }

    public T Get(MetadataBag bag)
    {
        return _getter(bag);
    }

    public void Set(MetadataBag bag, T value)
    {
        _setter(bag, value);
    }
}
