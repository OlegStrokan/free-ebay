using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Application.Consumers;
using Application.RetryStore;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Infrastructure.Kafka;

public sealed class KafkaConsumerBackgroundService(
    IOptions<KafkaOptions> kafkaOptions,
    IServiceProvider serviceProvider,
    ILogger<KafkaConsumerBackgroundService> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("CatalogService.Kafka");

    private const int MaxImmediateRetries = 3;
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    ];

    private readonly HashSet<TopicPartition> _pausedPartitions = new();

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
            await TryResumePausedPartitions(consumer, stoppingToken);

            ConsumeResult<string, string>? result = null;

            try
            {
                result = consumer.Consume(TimeSpan.FromMilliseconds(1000));
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

                var projected = await TryProjectWithRetries(eventType, wrapper, stoppingToken);

                if (projected)
                {
                    consumer.StoreOffset(result);
                    consumer.Commit(result);
                    continue;
                }

                // All 3 immediate retries failed - classify the failure
                await HandleExhaustedRetries(consumer, result, eventType, wrapper, stoppingToken);
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
                logger.LogError(ex, "Unexpected error in consumer loop");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        consumer.Close();
        logger.LogInformation("Kafka consumer stopped");
    }
    
    internal async Task<bool> TryProjectWithRetries(
        string eventType, EventWrapper wrapper, CancellationToken ct)
    {
        _lastProjectionException = null;

        for (var attempt = 0; attempt < MaxImmediateRetries; attempt++)
        {
            try
            {
                await DispatchAsync(eventType, wrapper.Payload, ct);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _lastProjectionException = ex;

                logger.LogWarning(
                    ex,
                    "Projection attempt {Attempt}/{Max} failed for event {EventType}",
                    attempt + 1, MaxImmediateRetries, eventType);

                if (attempt < MaxImmediateRetries - 1)
                {
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                    await Task.Delay(RetryDelays[attempt] + jitter, ct);
                }
            }
        }

        return false;
    }

    internal Exception? _lastProjectionException;

    // After all immediate retries are exhausted, classify the failure and either keep the message in kafka or move it to retry storage
    internal async Task HandleExhaustedRetries(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        string eventType,
        EventWrapper wrapper,
        CancellationToken ct)
    {
        var ex = _lastProjectionException!;
        var kind = FailureClassifier.Classify(ex);

        switch (kind)
        {
            case FailureKind.Systemic:
                logger.LogError(
                    ex,
                    "Systemic failure for {EventType} at Partition {P} Offset {O}. " +
                    "Pausing partition and keeping message in Kafka",
                    eventType, result.Partition.Value, result.Offset.Value);

                // Seek back so the message is re-delivered after resume
                consumer.Seek(new TopicPartitionOffset(result.TopicPartition, result.Offset));
                PausePartition(consumer, result.TopicPartition);
                break;

            case FailureKind.MessageSpecific:
                logger.LogWarning(
                    ex,
                    "Message-specific failure for {EventType} at Partition {P} Offset {O}. " +
                    "Moving to retry store",
                    eventType, result.Partition.Value, result.Offset.Value);

                var persisted = await TryPersistRetryRecord(result, eventType, wrapper, ex, ct);

                if (persisted)
                {
                    consumer.StoreOffset(result);
                    consumer.Commit(result);
                }
                else
                {
                    // Persistence failed - do NOT commit, seek back
                    logger.LogError(
                        "Failed to persist retry record for {EventType} at Partition {P} Offset {O}. " +
                        "NOT committing Kafka offset",
                        eventType, result.Partition.Value, result.Offset.Value);

                    consumer.Seek(new TopicPartitionOffset(result.TopicPartition, result.Offset));
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
                break;
        }
    }

    private async Task<bool> TryPersistRetryRecord(
        ConsumeResult<string, string> result,
        string eventType,
        EventWrapper wrapper,
        Exception ex,
        CancellationToken ct)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var retryStore = scope.ServiceProvider.GetRequiredService<IRetryStore>();

            var traceparent = GetHeader(result, "traceparent");
            var now = DateTime.UtcNow;

            var record = new RetryRecord
            {
                Id = Guid.NewGuid(),
                EventId = wrapper.EventId != Guid.Empty ? wrapper.EventId : null,
                EventType = eventType,
                Topic = result.Topic,
                Partition = result.Partition.Value,
                Offset = result.Offset.Value,
                MessageKey = result.Message.Key,
                Payload = result.Message.Value,
                Headers = SerializeHeaders(result.Message.Headers),
                FirstFailureTime = now,
                LastFailureTime = now,
                RetryCount = 0,
                NextRetryAt = now.AddMinutes(3),
                Status = RetryRecordStatus.Pending,
                LastErrorMessage = Truncate(ex.Message, 2000),
                LastErrorType = ex.GetType().FullName,
                CorrelationId = traceparent,
            };

            await retryStore.PersistAsync(record, ct);
            return true;
        }
        catch (Exception persistEx)
        {
            logger.LogError(persistEx, "Retry store persistence failed");
            return false;
        }
    }

    private void PausePartition(IConsumer<string, string> consumer, TopicPartition tp)
    {
        if (_pausedPartitions.Add(tp))
        {
            consumer.Pause([tp]);
            logger.LogWarning(
                "Paused partition {Topic}-{Partition} due to systemic Elasticsearch failure",
                tp.Topic, tp.Partition.Value);
        }
    }

    private async Task TryResumePausedPartitions(
        IConsumer<string, string> consumer, CancellationToken ct)
    {
        if (_pausedPartitions.Count == 0) return;

        // Probe Elasticsearch health
        var healthy = await IsElasticsearchHealthy(ct);
        if (!healthy) return;

        foreach (var tp in _pausedPartitions)
        {
            consumer.Resume([tp]);
            logger.LogInformation(
                "Resumed partition {Topic}-{Partition} — Elasticsearch is healthy again",
                tp.Topic, tp.Partition.Value);
        }

        _pausedPartitions.Clear();
    }

    private async Task<bool> IsElasticsearchHealthy(CancellationToken ct)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var indexer = scope.ServiceProvider
                .GetRequiredService<Application.Services.IElasticsearchIndexer>();

            // Attempt a lightweight check - use a no-op that confirms ES connectivity
            await indexer.DeleteAsync("__health_probe__", ct);
            return true;
        }
        catch
        {
            return false;
        }
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

    private static string? SerializeHeaders(Headers? headers)
    {
        if (headers is null || headers.Count == 0) return null;

        var dict = new Dictionary<string, string>();
        foreach (var h in headers)
        {
            dict[h.Key] = Encoding.UTF8.GetString(h.GetValueBytes());
        }
        return JsonSerializer.Serialize(dict);
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null ? null
        : value.Length <= maxLength ? value
        : value[..maxLength];
}
