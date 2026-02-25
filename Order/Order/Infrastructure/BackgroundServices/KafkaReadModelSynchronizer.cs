using System.Reflection;
using System.Text.Json;
using Confluent.Kafka;
using Domain.Common;
using Infrastructure.Services;
using Infrastructure.Services.EventIdempotencyChecker;

namespace Infrastructure.BackgroundServices;

// @todo: rewrite eventTypes discovery with type-safe mediatR style
// @think: sometimes i smell too much voodoo here. check how netflix handler this stuff

public sealed class KafkaReadModelSynchronizer : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<KafkaReadModelSynchronizer> logger;
    private readonly IConsumer<string, string> consumer;
    private readonly List<string> topics;
    private readonly Dictionary<string, Type> eventTypeMap;

    // @think: too much voodoo in constructor
    public KafkaReadModelSynchronizer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<KafkaReadModelSynchronizer> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;

        var kafkaConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = configuration["Kafka:ConsumerGroupId"] ?? "read-model-updater",
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
            configuration["Kafka:OrderEventsTopic"] ?? "order.events",
            configuration["Kafka:ReturnEventsTopic"] ?? "return.events"
        };

        // auto discover all event types at startup
        eventTypeMap = DiscoverEventTypes();

        logger.LogInformation(
            "Disovered {Count} event types: {EventTypes}",
            eventTypeMap.Count,
            string.Join(", ", eventTypeMap));
    }

    private static Dictionary<string, Type> DiscoverEventTypes()
    {
        var eventTypes = new Dictionary<string, Type>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            // skip system assemblies for performance
            if (assembly.FullName?.StartsWith("System") == true ||
                assembly.FullName?.StartsWith("Microsoft") == true)
                continue;


            try
            {
                var types = assembly.GetTypes()
                    .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) &&
                                !t.IsInterface && !t.IsAbstract);

                foreach (var type in types)
                {
                    eventTypes[type.Name] = type;
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // skip assemblies that can't be loaded
                continue;
            }
        }

        return eventTypes;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                        await ProcessMessageAsync(consumeResult, stoppingToken);

                        // manually commit offset after processing
                        consumer.StoreOffset(consumeResult);
                        consumer.Commit();
                    }
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming Kafka message");
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

        if (!eventTypeMap.TryGetValue(eventType, out var domainEventType))
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
    
    // route events to appropriate handlers using reflection
    private async Task RouteEventHandlerAsync(
        IDomainEvent domainEvent,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        
        // determine which updater to use based on event namespace/name
        object? updater = null;

        if (eventType.FullName?.Contains("Order") == true &&
            !eventType.FullName.Contains("Return"))
        {
            updater = scope.ServiceProvider.GetService<OrderReadModelUpdater>();
        } else if (eventType.FullName?.Contains("Return") == true)
        {   
            updater = scope.ServiceProvider.GetService<ReturnRequestReadModelUpdater>();
        }

        if (updater == null)
        {
            logger.LogWarning(
                "No updater found for event type {EventType",
                eventType.Name);
            return;
        }
        
        // findHandleAsync method
        // VOODOO
        var handleMethod = updater.GetType()
            .GetMethod("HandleAsync", new[] { eventType, typeof(CancellationToken) });

        if (handleMethod == null)
        {
            logger.LogWarning(
                "No HandleAsync method found for event type {EventType} of updater {Updater}",
                eventType.Name,
                updater.GetType().Name);
            return;
        }

        var task = handleMethod.Invoke(updater, new object?[] { domainEvent, cancellationToken }) as Task;

        if (task != null)
        {
            await task;
        }

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
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract EventId from event data");
        }

        return Guid.NewGuid();
    }

    public override void Dispose()
    {
        consumer?.Dispose();
        base.Dispose();
    }
}