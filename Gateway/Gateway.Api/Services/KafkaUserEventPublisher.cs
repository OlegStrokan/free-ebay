using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Gateway.Api.Services;

public interface IUserEventPublisher : IDisposable
{
    Task PublishAsync(string userId, string eventType, object payload, CancellationToken ct);
}

public sealed class KafkaUserEventPublisher : IUserEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaUserEventPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public KafkaUserEventPublisher(IConfiguration configuration, ILogger<KafkaUserEventPublisher> logger)
    {
        _logger = logger;
        _topic = configuration["Kafka:UserEventsTopic"] ?? "user.events";

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9093",
            EnableIdempotence = true,
            Acks = Acks.All,
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string userId, string eventType, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        var message = new Message<string, string>
        {
            Key = userId,
            Value = json,
            Headers = new Headers
            {
                { "event-type", Encoding.UTF8.GetBytes(eventType) }
            }
        };

        await _producer.ProduceAsync(_topic, message, ct);

        _logger.LogDebug("Published {EventType} for user {UserId}", eventType, userId);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
