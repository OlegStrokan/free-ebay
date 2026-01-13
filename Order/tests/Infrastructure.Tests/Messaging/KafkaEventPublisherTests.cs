using System.Text.Json;
using Application.DTOs;
using Confluent.Kafka;
using Domain.Entities;
using Domain.Events;
using Domain.ValueObjects;
using Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Infrastructure.Tests.Messaging;

public class KafkaEventPublisherTests
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly KafkaEventPublisher _sut;

    public KafkaEventPublisherTests()
    {
        _producer = Substitute.For<IProducer<string, string>>();
        _logger = Substitute.For<ILogger<KafkaEventPublisher>>();

        _sut = new KafkaEventPublisher(_producer, _logger);
    }

    [Fact]
    public async Task PublishAsync_ShouldSerializeAndProduce_Correctly()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var domainEvent = CreateOrderCreatedEvent(orderId, customerId);

        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string> { Partition = 0, Offset = 10 });

        await _sut.PublishAsync(domainEvent, CancellationToken.None);

        await _producer.Received(1).ProduceAsync(
            "order.events",
            Arg.Is<Message<string, string>>(msg =>
                msg.Key == orderId.ToString() &&
                msg.Value.Contains(orderId.ToString()) &&
                msg.Value.Contains("OrderCreatedEvent")
            ),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldMap_OrderCreatedEvent_To_DtoStructure()
    {
        var orderId = Guid.NewGuid();

        var domainEvent = CreateOrderCreatedEvent(orderId);
        
        Message<string, string> capturedMessage = null;

         _producer.ProduceAsync(
            Arg.Any<string>(),
            Arg.Do<Message<string, string>>(x => capturedMessage = x),
            Arg.Any<CancellationToken>()
            ).Returns(new DeliveryResult<string, string>());

        await _sut.PublishAsync(domainEvent, CancellationToken.None);

        Assert.NotNull(capturedMessage);

        var wrapper = JsonSerializer.Deserialize<EventWrapper>(capturedMessage.Value);
        Assert.NotNull(wrapper);
        Assert.Equal(nameof(OrderCreatedEvent), wrapper.EventType);

        var payloadDto = JsonSerializer.Deserialize<OrderCreatedEventDto>(wrapper.Payload);
        Assert.NotNull(payloadDto);
        Assert.Equal(orderId, payloadDto.OrderId);
        Assert.Equal("Street", payloadDto.DeliveryAddress.Street);
        Assert.Equal("EUR", payloadDto.Currency);
    }

    [Fact]
    public async Task PublishRawAsync_ShouldProduce_GenericMessage()
    {
        var eventId = Guid.NewGuid();
        var type = "OrderPaidEvent";
        var content = "{\"some\":\"json\"}";

        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>()).Returns(new DeliveryResult<string, string>());

        await _sut.PublishRawAsync(eventId, type, content, DateTime.UtcNow, CancellationToken.None);

        await _producer.Received(1).ProduceAsync(
            "order.events",
            Arg.Is<Message<string, string>>(msg =>
                msg.Key == eventId.ToString() &&
                msg.Headers.Any(h => h.Key == "event-type")
            ),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldLogError_AndRethrow_WhenKafkaFails()
    {
        var orderId = Guid.NewGuid();
        var domainEvent = CreateOrderCreatedEvent(orderId);
        
        var kafkaException = new ProduceException<string, string>(
            new Error(ErrorCode.Local_Transport), new DeliveryResult<string, string>());

        _producer.When(x => x.ProduceAsync(Arg.Any<string>(), 
            Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw kafkaException);

        await Assert.ThrowsAsync<ProduceException<string, string>>(() =>
            _sut.PublishAsync(domainEvent, CancellationToken.None));
    }

    private OrderCreatedEvent CreateOrderCreatedEvent(Guid orderId, Guid customerId = default)
    {
        var finalCustomerId = customerId == Guid.Empty ? Guid.NewGuid() : customerId;
        
        return new OrderCreatedEvent(
            OrderId.From(orderId),
            CustomerId.From(finalCustomerId),
            Money.Create(50, "EUR"),
            Address.Create("Street", "City", "Country", "180000"),
            new List<OrderItem>(),
            DateTime.UtcNow
        );
    }
    
    
    
}