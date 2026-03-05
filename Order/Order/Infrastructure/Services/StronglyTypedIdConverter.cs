namespace Infrastructure.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

// don't need to be registered for DI, cuz we pinned it as private static method for event store repository
// @think: i hate an asshole who wrote this shitty code heavily based on reflection 
public class StronglyTypedIdConverter<TId> : JsonConverter<TId>
{
    public override TId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return default;

        var value = reader.GetGuid();
        
        // Use reflection to call your private constructor
        return (TId)Activator.CreateInstance(
            typeToConvert, 
            BindingFlags.Instance | BindingFlags.NonPublic, 
            null, 
            [value], 
            null)!;
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        var prop = value?.GetType().GetProperty("Value");
        if (prop != null)
        {
            var rawValue = prop.GetValue(value);
        
            if (rawValue is Guid guid)
            {
                writer.WriteStringValue(guid);
            }
            else if (Guid.TryParse(rawValue?.ToString(), out var parsedGuid))
            {
                writer.WriteStringValue(parsedGuid);
            }
        }
    }
}