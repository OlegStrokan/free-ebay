using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Entities.Subscription;
using Domain.ValueObjects;

namespace Infrastructure.Services;

public sealed class RecurringOrderItemJsonConverter : JsonConverter<RecurringOrderItem>
{
    public override RecurringOrderItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject for RecurringOrderItem.");

        ProductId? productId = null;
        int quantity = 0;
        Money? price = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName inside RecurringOrderItem.");

            var propName = reader.GetString();
            reader.Read();

            switch (propName)
            {
                case "ProductId":
                    productId = JsonSerializer.Deserialize<ProductId>(ref reader, options);
                    break;
                case "Quantity":
                    quantity = reader.GetInt32();
                    break;
                case "Price":
                    price = JsonSerializer.Deserialize<Money>(ref reader, options);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (productId is null || price is null)
            throw new JsonException("RecurringOrderItem JSON is missing required properties (ProductId or Price).");

        return RecurringOrderItem.Create(productId, quantity, price);
    }

    public override void Write(Utf8JsonWriter writer, RecurringOrderItem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("ProductId");
        JsonSerializer.Serialize(writer, value.ProductId, options);

        writer.WriteNumber("Quantity", value.Quantity);

        writer.WritePropertyName("Price");
        JsonSerializer.Serialize(writer, value.Price, options);

        writer.WriteEndObject();
    }
}
