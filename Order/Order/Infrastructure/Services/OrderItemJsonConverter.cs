using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Entities.Order;
using Domain.ValueObjects;

namespace Infrastructure.Services;

// handle deserialization of OrderItem whose constructor is private.
public sealed class OrderItemJsonConverter : JsonConverter<OrderItem>
{
    public override OrderItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject for OrderItem.");

        OrderItemId? id = null;
        OrderId?     orderId = null;
        ProductId?   productId = null;
        int          quantity = 0;
        Money?       price = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName inside OrderItem.");

            var propName = reader.GetString();
            reader.Read();

            switch (propName)
            {
                case "Id":
                    id = JsonSerializer.Deserialize<OrderItemId>(ref reader, options);
                    break;
                case "OrderId":
                    if (reader.TokenType != JsonTokenType.Null)
                        orderId = JsonSerializer.Deserialize<OrderId>(ref reader, options);
                    break;
                case "ProductId":
                    productId = JsonSerializer.Deserialize<ProductId>(ref reader, options);
                    break;
                case "Quantity":
                    quantity = reader.GetInt32();
                    break;
                case "PriceAtPurchase":
                    price = JsonSerializer.Deserialize<Money>(ref reader, options);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (id is null || productId is null || price is null)
            throw new JsonException("OrderItem JSON is missing required properties (Id, ProductId, or PriceAtPurchase).");

        return OrderItem.FromSnapshot(new OrderItemSnapshotState(
            ItemId:    id!.Value,
            OrderId:   orderId?.Value,
            ProductId: productId!.Value,
            Quantity:  quantity,
            Price:     price!.Amount,
            Currency:  price.Currency));
    }

    public override void Write(Utf8JsonWriter writer, OrderItem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Id");
        JsonSerializer.Serialize(writer, value.Id, options);

        writer.WritePropertyName("OrderId");
        JsonSerializer.Serialize(writer, value.OrderId, options);

        writer.WritePropertyName("ProductId");
        JsonSerializer.Serialize(writer, value.ProductId, options);

        writer.WriteNumber("Quantity", value.Quantity);

        writer.WritePropertyName("PriceAtPurchase");
        JsonSerializer.Serialize(writer, value.PriceAtPurchase, options);

        writer.WriteEndObject();
    }
}
