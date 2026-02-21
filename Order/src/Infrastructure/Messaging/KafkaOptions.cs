namespace Infrastructure.Messaging;


//@todo: use it in every "configuration[""]"
public class KafkaOptions
{
    public const string SectionName = "Kafka";
    
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "order-service";
    public string SagaTopic { get; set; } = "order.events";
    public string OrderEventsTopic { get; set; } = "order.events";
    public string ReturnEventsTopic { get; set; } = "return.events";
    public bool EnableAutoCommit { get; set; } = false;
}