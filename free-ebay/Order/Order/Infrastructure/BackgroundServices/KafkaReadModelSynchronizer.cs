using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Domain.Common;
using Infrastructure.Messaging;
using Infrastructure.Services;
using Infrastructure.Services.EventIdempotencyChecker;
using Microsoft.Extensions.Options;

namespace Infrastructure.BackgroundServices;

// reflection based code sometimes can smell to much voodoo, but many cqrs framwework do the same
// kafka has no auto-instrumentation build for opentelemetry so we traced it manually
public sealed class KafkaReadModelSynchronizer : BackgroundService
{
    private static readonly ActivitySource _activitySource = new("OrderService.Kafka");

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<KafkaReadModelSynchronizer> logger;
    private readonly IConsumer<string, string> consumer;
    private readonly List<string> topics;
    private readonly IDomainEventTypeRegistry eventTypeRegistry;
    private readonly IReadModelHandlerRegistry handlerRegistry;

    public KafkaReadModelSynchronizer(
        IServiceProvider serviceProvider,
        IOptions<KafkaOptions> kafkaOptions,
        IDomainEventTypeRegistry eventTypeRegistry,
        IReadModelHandlerRegistry handlerRegistry,
        ILogger<KafkaReadModelSynchronizer> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.eventTypeRegistry = eventTypeRegistry;
        this.handlerRegistry = handlerRegistry;

        var opts = kafkaOptions.Value;

        var kafkaConfig = new ConsumerConfig
        {
            BootstrapServers = opts.BootstrapServers,
            GroupId = "read-model-updater",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            IsolationLevel = IsolationLevel.ReadCommitted
        };

        consumer = new ConsumerBuilder<string, string>(kafkaConfig)
            .SetErrorHandler((_e, error) => { logger.LogError("Kafka consumer error: {Error}", error.Reason); })
            .Build();

        topics = new List<string>
        {
            opts.OrderEventsTopic,
            opts.ReturnEventsTopic
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        
        consumer.Subscribe(topics);

        logger.LogInformation("Kafka read model synchronizer started. Subscribed to topics: {Topics}",
            string.Join(", ", topics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    if (consumeResult?.Message != null)
                    {
                        // restore trace context propagated from the publisher
                        Activity? activity = null;
                        var traceHeader = consumeResult.Message.Headers
                            .FirstOrDefault(h => h.Key == "traceparent");
                        if (traceHeader != null)
                        {
                            var traceparent = Encoding.UTF8.GetString(traceHeader.GetValueBytes());
                            if (ActivityContext.TryParse(traceparent, null, out var parentCtx))
                                activity = _activitySource.StartActivity(
                                    "kafka.consume.read-model", ActivityKind.Consumer, parentCtx);
                        }

                        try
                        {
                            await ProcessMessageAsync(consumeResult, stoppingToken);
                        }
                        finally
                        {
                            activity?.Dispose();
                        }

                        // manually commit offset after processing
                        consumer.StoreOffset(consumeResult);
                        consumer.Commit();
                    }
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming Kafka message");
                }
                catch (OperationCanceledException)
                {
                    // stoppingToken was cancelled — exit the loop cleanly
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing Kafka message");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
        
        logger.LogInformation("Kafka read model synchronizer stopped");
    }

    private async Task ProcessMessageAsync(
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        var eventTypeHeader = consumeResult.Message.Headers
            .FirstOrDefault(h => h.Key == "event-type");

        if (eventTypeHeader == null)
        {
            logger.LogWarning(
                "Message missing event-type header. Offset: {Offset}",
                consumeResult.Offset);
            return;
        }

        var eventType = System.Text.Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes());
        var eventData = consumeResult.Message.Value;

        logger.LogDebug(
            "Processing event {EventType} from topic {Topic} at offset {Offset}",
            eventType,
            consumeResult.Topic,
            consumeResult.Offset);
        
        try
        {
            await HandleEventDynamicallyAsync(eventType, eventData, cancellationToken);
            
            logger.LogInformation(
                "Successfully processed event {EventType} at offset {Offset}", eventType, consumeResult.Offset);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process event {EventType} at offset {Offset}. Will retry.",
                eventType,
                consumeResult.Offset);
            throw; // rethrow to prevent commit
        }
    }


    private async Task HandleEventDynamicallyAsync(
        string eventType,
        string eventData,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var idempotencyChecker = scope.ServiceProvider.GetRequiredService<IEventIdempotencyChecker>();

        var eventId = ExtractEventId(eventData);
        if (await idempotencyChecker.HasBeenProcessedAsync(eventId, cancellationToken))
        {
            logger.LogInformation(
                "Event {EventId} ({EventType}) already processed. Skipping duplicate.", eventId, eventType);
            return;
        }

        if (!eventTypeRegistry.TryGetType(eventType, out var domainEventType))
        {
            logger.LogWarning(
                "Unknown event type {EventType}. Skipping.", eventType);
            return;
        }

        var domainEvent = JsonSerializer.Deserialize(eventData, domainEventType) as IDomainEvent;

        if (domainEvent == null)
        {
            logger.LogError("Failed to deserialize event {EventType}", eventType);
            return;
        }

        await RouteEventHandlerAsync(domainEvent, scope, cancellationToken);

        await idempotencyChecker.MarkAsProcessedAsync(eventId, eventType, cancellationToken);

    }
    
    /// <summary>
    /// Route events to appropriate handlers using cached handler registry (no reflection).
    /// Each updater type gets cached delegates for each event type on first use.
    /// </summary>
    private async Task RouteEventHandlerAsync(
        IDomainEvent domainEvent,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();

        var updater = scope.ServiceProvider
            .GetServices<IReadModelUpdater>()
            .FirstOrDefault(u => u.CanHandle(eventType));

        if (updater == null)
        {
            logger.LogWarning(
                "No updater found for event type {EventType}",
                eventType.Name);
            return;
        }

        // Use cached handler registry instead of reflection
        await handlerRegistry.HandleAsync(domainEvent, updater, cancellationToken);

        logger.LogDebug(
            "Event {EventType} handled by {Updater}",
            eventType.Name,
            updater.GetType().Name);
    }

    private Guid ExtractEventId(string eventData)
    {
        try
        {
            using var doc = JsonDocument.Parse(eventData);
            if (doc.RootElement.TryGetProperty("EventId", out var eventIdElement))
                return Guid.Parse(eventIdElement.GetString()!);

            throw new InvalidOperationException("Message is missing a valid 'EventId' property");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract EventId from event data");
            throw;
        }
    }

    public override void Dispose()
    {
        consumer?.Dispose();
        base.Dispose();
    }
}