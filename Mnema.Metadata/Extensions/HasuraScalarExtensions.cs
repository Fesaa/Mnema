namespace Mnema.Metadata.Extensions;

using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Serialization;

public class BigIntSerializer() : ScalarSerializer<long>("bigint");

public class Float8Serializer() : ScalarSerializer<float>("float8");

public class NumericSerializer() : ScalarSerializer<decimal>("numeric");

public class JsonElementSerializer(string name = "json") : ScalarSerializer<JsonElement>(name);

public class JsonbElementSerializer() : JsonElementSerializer("jsonb");

public class DateTimeSerializer() : ScalarSerializer<string, DateTime>("date")
{
    public override DateTime Parse(string serializedValue)
    {
        return DateTime.Parse(serializedValue);
    }

    protected override string Format(DateTime runtimeValue)
    {
        return runtimeValue.ToString("O");
    }
}

public static class HasuraScalarExtensions
{
    public static IServiceCollection AddHasuraScalars(this IServiceCollection builder)
    {
        return builder
            .AddSerializer<BigIntSerializer>()
            .AddSerializer<Float8Serializer>()
            .AddSerializer<NumericSerializer>()
            .AddSerializer<JsonElementSerializer>()
            .AddSerializer<JsonbElementSerializer>()
            .AddSerializer<DateTimeSerializer>();
    }
}
