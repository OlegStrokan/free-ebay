namespace Infrastructure.Messaging;

public class KafkaOptions
{
    public string BootstrapSettings { get; set; } = string.Empty;
    public string ConsumerGroupId { get; set; } = "test-group";
    public bool AutoOffsetReset { get; set; } = true;
}