namespace Mnema.Common.Helpers;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class FlexibleBooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.TryGetInt32(out int value)
                ? value != 0
                : throw new JsonException("Invalid number for boolean."),
            JsonTokenType.String => bool.TryParse(reader.GetString(), out bool b) ? b :
                reader.GetString() == "1",
            _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing boolean.")
        };
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
