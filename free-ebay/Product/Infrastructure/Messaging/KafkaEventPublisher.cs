using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Confluent.Kafka;
using Domain.Common;
using Domain.Events;
using Microsoft.Extensions.Options;

namespace Infrastructure.Messaging;

// Kafka has no auto-instrumentation for OpenTelemetry, so we trace manually
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private static readonly ActivitySource ActivitySource = new("ProductService.Kafka");

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly KafkaOptions _options;

    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        _options = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            EnableIdempotence = true,
            MaxInFlight = 1,
            Acks = Acks.All,
            MessageSendMaxRetries = 10
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    // Constructor for testing — allows injecting a pre-built producer
    internal KafkaEventPublisher(
        IProducer<string, string> producer,
        ILogger<KafkaEventPublisher> logger,
        KafkaOptions? options = null)
    {
        _producer = producer;
        _logger = logger;
        _options = options ?? new KafkaOptions();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IDomainEvent
    {
        try
        {
            var eventType = @event.GetType().Name;
            var wrapper = new EventWrapper
            {
                EventId    = @event.EventId,
                EventType  = eventType,
                Payload    = JsonSerializer.Serialize(@event, @event.GetType()),
                OccurredOn = @event.OccurredOn
            };

            var message = BuildMessage(GetEventKey(@event), eventType, @event.EventId, wrapper);

            var result = await _producer.ProduceAsync(_options.ProductEventsTopic, message, ct);
            _logger.LogInformation(
                "Published {EventType} to Kafka Partition: {Partition}, Offset: {Offset}",
                eventType, result.Partition, result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", @event.GetType().Name);
            throw;
        }
    }

    public async Task PublishRawAsync(Guid id, string typeName, string content,
                                      DateTime occurredOn, string aggregateId, CancellationToken ct = default)
    {
        try
        {
            var wrapper = new EventWrapper
            {
                EventId    = id,
                EventType  = typeName,
                Payload    = content,
                OccurredOn = occurredOn
            };

            var message = BuildMessage(aggregateId, typeName, id, wrapper);

            var result = await _producer.ProduceAsync(_options.ProductEventsTopic, message, ct);
            _logger.LogInformation(
                "Published {EventType} to Kafka Partition: {Partition}, Offset: {Offset}",
                typeName, result.Partition, result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeName);
            throw;
        }
    }

    private static Message<string, string> BuildMessage(
        string key, string eventType, Guid eventId, EventWrapper wrapper)
    {
        var headers = new Headers
        {
            { "event-type", Encoding.UTF8.GetBytes(eventType) },
            { "event-id",   Encoding.UTF8.GetBytes(eventId.ToString()) }
        };

        if (Activity.Current is not null)
            headers.Add("traceparent",
                Encoding.UTF8.GetBytes(
                    $"00-{Activity.Current.TraceId}-{Activity.Current.SpanId}-01"));

        return new Message<string, string>
        {
            Key     = key,
            Value   = JsonSerializer.Serialize(wrapper),
            Headers = headers
        };
    }

    private static string GetEventKey<TEvent>(TEvent @event) where TEvent : IDomainEvent =>
        @event switch
        {
            ProductCreatedEvent e => e.ProductId.Value.ToString(),
            ProductUpdatedEvent e => e.ProductId.Value.ToString(),
            ProductStockUpdatedEvent e => e.ProductId.Value.ToString(),
            ProductStatusChangedEvent e => e.ProductId.Value.ToString(),
            ProductDeletedEvent e => e.ProductId.Value.ToString(),
            _  => @event.EventId.ToString()
        };

    public void Dispose() => _producer.Dispose();
}
