using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Confluent.Kafka;
using Domain.Common;
using Domain.Events;

namespace Infrastructure.Messaging;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        // todo: override with envs
        var config = new ProducerConfig
        {
            BootstrapServers = "localhost:9092",
            EnableIdempotence = true,
            MaxInFlight = 1,
            Acks = Acks.All,
            MessageSendMaxRetries = 10
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        try
        {
            var eventType = @event.GetType().Name;

            var eventWrapper = new EventWrapper
            {
                EventId = @event.EventId,
                EventType = eventType,
                Payload = SerializeEvent(@event),
                OccurredOn = @event.OccurredOn
            };

            var message = new Message<string, string>
            {
                Key = GetEventKey(@event),
                Value = JsonSerializer.Serialize(eventWrapper),
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(eventType) },
                    { "event-id", System.Text.Encoding.UTF8.GetBytes(@event.EventId.ToString()) }
                }
            };

            var result = await _producer.ProduceAsync("order.events", message, cancellationToken);

            _logger.LogInformation(
                "Published {EventType} to Kafka Partition: {Partition}, Offset: {Offset}",
                eventType,
                result.Partition,
                result.Offset);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", @event.GetType().Name);
            throw;
        }
}

    public async Task PublishRawAsync(Guid eventId, string typeName, string content, DateTime occuredOn, CancellationToken cancellationToken)
    {
        try
        {

            var eventWrapper = new EventWrapper
            {
                EventId = eventId,
                EventType = typeName,
                Payload = content,
                OccurredOn = occuredOn
            };

            var finalJson = JsonSerializer.Serialize(eventWrapper);

            var message = new Message<string, string>
            {
                Key = eventId.ToString(),
                Value = finalJson,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(typeName) },
                    { "event-id", System.Text.Encoding.UTF8.GetBytes(eventId.ToString()) }
                }
            };

            var result = await _producer.ProduceAsync("order.events", message, cancellationToken);

            _logger.LogInformation(
                "Published {EventType} to Kafka Partition: {Partition}, Offset: {Offset}",
                typeName,
                result.Partition,
                result.Offset);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeName);
            throw;
        }
    }


    private string SerializeEvent<TEvent>(TEvent @event) where TEvent : IDomainEvent
    {
        return @event switch
        {
            OrderCreatedEvent e => System.Text.Json.JsonSerializer.Serialize(new OrderCreatedEventDto
            {
                OrderId = e.OrderId.Value,
                CustomerId = e.CustomerId.Value,
                TotalAmount = e.TotalPrice.Amount,
                Currency = e.TotalPrice.Currency,
                DeliveryAddress = new AddressDto(
                    e.DeliveryAddress.Street,
                    e.DeliveryAddress.City,
                    e.DeliveryAddress.Country,
                    e.DeliveryAddress.PostalCode
                ),
                Items = e.Items.Select(i => new OrderItemDto(
                    i.ProductId.Value,
                    i.Quantity,
                    i.PriceAtPurchase.Amount,
                    i.PriceAtPurchase.Currency
                )).ToList(),
                CreatedAt = e.CreatedAt
            }),

            _ => System.Text.Json.JsonSerializer.Serialize(@event)
        };
    }


    private string GetEventKey<TEvent>(TEvent @event) where TEvent : IDomainEvent
    {
        return @event switch
        {
            OrderCreatedEvent e => e.OrderId.Value.ToString(),
            OrderPaidEvent e => e.OrderId.Value.ToString(),
            OrderApprovedEvent e => e.OrderId.Value.ToString(),
            OrderCompletedEvent e => e.OrderId.Value.ToString(),
            OrderCancelledEvent e => e.OrderId.Value.ToString(),
            _ => @event.EventId.ToString()
        };
    }

    public void Dispose()
    {
        _producer?.Flush();
        _producer?.Dispose();
    }
}