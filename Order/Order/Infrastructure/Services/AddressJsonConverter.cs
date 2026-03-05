using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.ValueObjects;

namespace Infrastructure.Services;

// handle deserialization of Address whose constructor is private.
public sealed class AddressJsonConverter : JsonConverter<Address>
{
    public override Address Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject for Address.");

        string? street = null, city = null, country = null, postalCode = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName inside Address object.");

            var propName = reader.GetString();
            reader.Read();

            switch (propName)
            {
                case "Street":     street     = reader.GetString(); break;
                case "City":       city       = reader.GetString(); break;
                case "Country":    country    = reader.GetString(); break;
                case "PostalCode": postalCode = reader.GetString(); break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (street is null || city is null || country is null || postalCode is null)
            throw new JsonException("Address is missing required properties.");

        return Address.Create(street, city, country, postalCode);
    }

    public override void Write(Utf8JsonWriter writer, Address value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Street",     value.Street);
        writer.WriteString("City",       value.City);
        writer.WriteString("Country",    value.Country);
        writer.WriteString("PostalCode", value.PostalCode);
        writer.WriteEndObject();
    }
}
