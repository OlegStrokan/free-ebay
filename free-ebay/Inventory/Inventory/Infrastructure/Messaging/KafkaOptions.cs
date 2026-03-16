namespace Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = "localhost:9092";

    public string InventoryEventsTopic { get; init; } = "inventory.events";

    public string ClientId { get; init; } = "inventory-service";
}
