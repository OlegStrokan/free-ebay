using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Application.Consumers;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Infrastructure.Kafka;

public sealed class KafkaConsumerBackgroundService(
    IOptions<KafkaOptions> kafkaOptions,
    IServiceProvider serviceProvider,
    ILogger<KafkaConsumerBackgroundService> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("CatalogService.Kafka");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var options = kafkaOptions.Value;

        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            IsolationLevel = IsolationLevel.ReadCommitted,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(options.ProductEventsTopic);

        logger.LogInformation(
            "Kafka consumer started. Topic: {Topic}, GroupId: {GroupId}",
            options.ProductEventsTopic, options.ConsumerGroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(1000));
                if (result is null) continue;

                var eventType = GetHeader(result, "event-type");
                if (string.IsNullOrEmpty(eventType))
                {
                    logger.LogWarning(
                        "Message at Partition {P} Offset {O} has no event-type header — skipping",
                        result.Partition.Value, result.Offset.Value);
                    consumer.StoreOffset(result);
                    consumer.Commit(result);
                    continue;
                }

                var wrapper = JsonSerializer.Deserialize<EventWrapper>(result.Message.Value);
                if (wrapper is null)
                {
                    logger.LogWarning(
                        "Failed to deserialize EventWrapper for event type '{EventType}' — skipping",
                        eventType);
                    consumer.StoreOffset(result);
                    consumer.Commit(result);
                    continue;
                }

                // manually trace kafka
                ActivityContext parentCtx = default;
                var traceparent = GetHeader(result, "traceparent");
                if (traceparent is not null)
                    ActivityContext.TryParse(traceparent, null, out parentCtx);

                using var activity = ActivitySource.StartActivity(
                    $"Consume:{eventType}", ActivityKind.Consumer, parentCtx);
                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination", options.ProductEventsTopic);
                activity?.SetTag("messaging.kafka.consumer.group", options.ConsumerGroupId);

                await DispatchAsync(eventType, wrapper.Payload, stoppingToken);

                consumer.StoreOffset(result);
                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing Kafka message");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("Kafka consumer stopped");
    }

    internal async Task DispatchAsync(string eventType, JsonElement payload, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var consumers = scope.ServiceProvider.GetRequiredService<IEnumerable<IProductEventConsumer>>();

        var handler = consumers.FirstOrDefault(c => c.EventType == eventType);
        if (handler is null)
        {
            logger.LogDebug("No consumer registered for event type '{EventType}' — skipping", eventType);
            return;
        }

        await handler.ConsumeAsync(payload, ct);
    }

    private static string? GetHeader(ConsumeResult<string, string> result, string key)
    {
        var header = result.Message.Headers.FirstOrDefault(h => h.Key == key);
        return header is null ? null : Encoding.UTF8.GetString(header.GetValueBytes());
    }
}
