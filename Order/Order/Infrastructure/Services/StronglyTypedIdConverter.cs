namespace Infrastructure.Services;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// detects strongly-typed ID records that expose a single  property
// of type Guid, long, string
public sealed class StronglyTypedIdConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        var valueProp = typeToConvert.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        if (valueProp is null) return false;

        if (valueProp.PropertyType != typeof(Guid)
            && valueProp.PropertyType != typeof(string)
            && valueProp.PropertyType != typeof(long))
            return false;

        // must expose a static From(TValue) factory method
        return typeToConvert.GetMethod(
            "From", BindingFlags.Public | BindingFlags.Static,
            null, [valueProp.PropertyType], null) is not null;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert
            .GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)!
            .PropertyType;

        var converterType = valueType switch
        {
            _ when valueType == typeof(Guid)   => typeof(GuidStronglyTypedIdConverter<>),
            _ when valueType == typeof(string) => typeof(StringStronglyTypedIdConverter<>),
            _ when valueType == typeof(long)   => typeof(LongStronglyTypedIdConverter<>),
            _ => throw new NotSupportedException($"Unsupported strongly-typed ID value type: {valueType}")
        };

        return (JsonConverter)Activator.CreateInstance(
            converterType.MakeGenericType(typeToConvert))!;
    }
}

internal sealed class GuidStronglyTypedIdConverter<TId> : JsonConverter<TId>
{
    private static readonly MethodInfo FromMethod =
        typeof(TId).GetMethod("From", BindingFlags.Public | BindingFlags.Static,
            null, [typeof(Guid)], null)!;

    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return default!;

        // raw GUID string (e.g. legacy / alternative formats)
        if (reader.TokenType == JsonTokenType.String)
            return Invoke(reader.GetGuid());

        // object format: {"Value": "guid-string"}
        reader.Read(); // StartObject -> PropertyName ("Value")
        reader.Read(); // PropertyName -> String value
        var guid = reader.GetGuid();
        reader.Read(); // String -> EndObject
        return Invoke(guid);
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        var guid = (Guid)typeof(TId).GetProperty("Value")!.GetValue(value)!;
        writer.WriteStartObject();
        writer.WriteString("Value", guid);
        writer.WriteEndObject();
    }

    private static TId Invoke(Guid guid) => (TId)FromMethod.Invoke(null, [guid])!;
}

internal sealed class StringStronglyTypedIdConverter<TId> : JsonConverter<TId>
{
    private static readonly MethodInfo FromMethod =
        typeof(TId).GetMethod("From", BindingFlags.Public | BindingFlags.Static,
            null, [typeof(string)], null)!;

    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return default!;

        // raw string value
        if (reader.TokenType == JsonTokenType.String)
            return Invoke(reader.GetString()!);

        // object format: {"Value": "string-value"}
        reader.Read(); // StartObject -> PropertyName
        reader.Read(); // PropertyName -> String value
        var str = reader.GetString()!;
        reader.Read(); // String -> EndObject
        return Invoke(str);
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        var str = (string)typeof(TId).GetProperty("Value")!.GetValue(value)!;
        writer.WriteStartObject();
        writer.WriteString("Value", str);
        writer.WriteEndObject();
    }

    private static TId Invoke(string str) => (TId)FromMethod.Invoke(null, [str])!;
}

internal sealed class LongStronglyTypedIdConverter<TId> : JsonConverter<TId>
{
    private static readonly MethodInfo FromMethod =
        typeof(TId).GetMethod("From", BindingFlags.Public | BindingFlags.Static,
            null, [typeof(long)], null)!;

    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return default!;

        // raw number value
        if (reader.TokenType == JsonTokenType.Number)
            return Invoke(reader.GetInt64());

        // object format: {"Value": 123}
        reader.Read(); // StartObject -> PropertyName
        reader.Read(); // PropertyName -> Number value
        var num = reader.GetInt64();
        reader.Read(); // Number -> EndObject
        return Invoke(num);
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        var num = (long)typeof(TId).GetProperty("Value")!.GetValue(value)!;
        writer.WriteStartObject();
        writer.WriteNumber("Value", num);
        writer.WriteEndObject();
    }

    private static TId Invoke(long num) => (TId)FromMethod.Invoke(null, [num])!;
}