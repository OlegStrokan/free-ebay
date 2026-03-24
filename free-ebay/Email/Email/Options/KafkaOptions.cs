namespace Email.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "email-service";
    public string EmailEventsTopic { get; set; } = "email.events";
    public string EmailDlqTopic { get; set; } = "email.events.dlq";
    public bool EnableAutoCommit { get; set; } = false;
    public int MaxDeliveryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 500;
}