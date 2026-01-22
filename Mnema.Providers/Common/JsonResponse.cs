using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Mnema.Providers.Common;

public class JsonAccessor
{
    private readonly JsonElement _root;

    public JsonAccessor(JsonElement root) => _root = root;
    public JsonAccessor(string json) => _root = JsonDocument.Parse(json).RootElement;

    public string SelectString(string path)
    {
        var element = Navigate(path);
        return element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : "";
    }

    /// <summary>
    /// Select a raw JsonElement for custom processing.
    /// Returns a JsonElement with ValueKind.Undefined if not found.
    /// </summary>
    public JsonAccessor Select(string path)
    {
        return new JsonAccessor(Navigate(path));
    }

    /// <summary>
    /// Select multiple JsonElements for custom processing.
    /// Returns an empty enumerable if the path is invalid.
    /// </summary>
    public IEnumerable<JsonAccessor> SelectMany(string path)
    {
        return NavigateMany(path).Select(node => new JsonAccessor(node));
    }

    public IEnumerable<string> SelectManyString(string path)
    {
        return NavigateMany(path)
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString() ?? "");
    }

    public int SelectInt(string path)
    {
        var element = Navigate(path);
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var val) ? val : 0;
    }

    public IEnumerable<int> SelectManyInt(string path)
    {
        return NavigateMany(path)
            .Where(e => e.ValueKind == JsonValueKind.Number)
            .Select(e => e.TryGetInt32(out var val) ? val : 0);
    }

    public bool SelectBool(string path)
    {
        var element = Navigate(path);
        return element.ValueKind is JsonValueKind.True or JsonValueKind.False && element.GetBoolean();
    }

    public T? SelectAs<T>(string path = "")
    {
        var element = string.IsNullOrEmpty(path) ? _root : Navigate(path);
        if (element.ValueKind == JsonValueKind.Undefined || element.ValueKind == JsonValueKind.Null)
            return default;

        return JsonSerializer.Deserialize<T>(element.GetRawText());
    }

    private JsonElement Navigate(string path)
    {
        if (string.IsNullOrEmpty(path)) return _root;

        var parts = path.Split('.');
        var current = _root;

        foreach (var part in parts)
        {
            if (current.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return default;

            if (part.Contains("[") && part.Contains("]"))
            {
                var propName = part.Substring(0, part.IndexOf('['));
                var indexStr = part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1);

                if (!string.IsNullOrEmpty(propName))
                {
                    if (!current.TryGetProperty(propName, out current)) return default;
                }

                if (int.TryParse(indexStr, out int index) && current.ValueKind == JsonValueKind.Array)
                {
                    if (index >= 0 && index < current.GetArrayLength())
                        current = current[index];
                    else
                        return default;
                }
                else return default;
            }
            else
            {
                if (!current.TryGetProperty(part, out current)) return default;
            }
        }

        return current;
    }

    private IEnumerable<JsonElement> NavigateMany(string path)
    {
        if (string.IsNullOrEmpty(path)) return [_root];

        var parts = path.Split('.');
        var currentSet = new List<JsonElement> { _root };

        foreach (var part in parts)
        {
            var nextSet = new List<JsonElement>();

            foreach (var current in currentSet)
            {
                if (current.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                    continue;

                if (part == "*")
                {
                    if (current.ValueKind == JsonValueKind.Array)
                        nextSet.AddRange(current.EnumerateArray());
                }
                else if (part.Contains("[*]"))
                {
                    var propName = part.Replace("[*]", "");
                    if (current.TryGetProperty(propName, out var array) && array.ValueKind == JsonValueKind.Array)
                        nextSet.AddRange(array.EnumerateArray());
                }
                else if (part.Contains('[') && part.Contains(']'))
                {
                    var propName = part[..part.IndexOf('[')];
                    var indexStr = part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1);

                    var target = current;
                    if (!string.IsNullOrEmpty(propName))
                    {
                        if (!current.TryGetProperty(propName, out target)) continue;
                    }

                    if (int.TryParse(indexStr, out int index) && target.ValueKind == JsonValueKind.Array)
                    {
                        if (index >= 0 && index < target.GetArrayLength())
                            nextSet.Add(target[index]);
                    }
                }
                else
                {
                    if (current.TryGetProperty(part, out var next))
                        nextSet.Add(next);
                }
            }
            currentSet = nextSet;
        }

        return currentSet;
    }
}
