namespace Infrastructure.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "catalog-service";
    public string ProductEventsTopic { get; set; } = "product.events";
}
