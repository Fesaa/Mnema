using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mnema.Models.DTOs.UI;

namespace Mnema.Server.Helpers;

public sealed class FormFieldDefinitionConverter : JsonConverter<FormFieldDefinition>
{
    public override void Write(
        Utf8JsonWriter writer,
        FormFieldDefinition value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

    public override FormFieldDefinition Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}
