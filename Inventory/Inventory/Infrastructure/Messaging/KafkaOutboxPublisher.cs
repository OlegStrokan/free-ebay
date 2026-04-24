using Confluent.Kafka;
using Infrastructure.Persistence.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public sealed class KafkaOutboxPublisher(
    IProducer<string, string> producer,
    ILogger<KafkaOutboxPublisher> logger) : IOutboxPublisher
{
    public async Task PublishAsync(OutboxMessageEntity message, CancellationToken cancellationToken)
    {
        await producer.ProduceAsync(
            message.Topic,
            new Message<string, string>
            {
                Key = message.OutboxMessageId.ToString(),
                Value = message.Payload
            },
            cancellationToken);

        logger.LogInformation(
            "Outbox message published. MessageId={MessageId}, EventType={EventType}, Topic={Topic}",
            message.OutboxMessageId,
            message.EventType,
            message.Topic);
    }
}
