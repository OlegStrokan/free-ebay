using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Domain.Common;
using Domain.Events;
using Domain.ValueObjects;
using Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Infrastructure.Tests.Messaging;

[TestFixture]
public class KafkaEventPublisherTests
{
    private IProducer<string, string> _producer;
    private ILogger<KafkaEventPublisher> _logger;
    private KafkaEventPublisher _sut;
    private KafkaOptions _options;

    [SetUp]
    public void SetUp()
    {
        _producer = Substitute.For<IProducer<string, string>>();
        _logger = Substitute.For<ILogger<KafkaEventPublisher>>();
        _options = new KafkaOptions { ProductEventsTopic = "product.events" };

        _sut = new KafkaEventPublisher(_producer, _logger, _options);

        _producer
            .ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string> { Partition = 0, Offset = 0 });
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
        _producer.Dispose();
    }

    #region PublishAsync

    [Test]
    public async Task PublishAsync_ShouldProduceToConfiguredTopic()
    {
        var evt = CreateProductCreatedEvent();

        await _sut.PublishAsync(evt);

        await _producer.Received(1).ProduceAsync(
            "product.events",
            Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishAsync_ShouldUseProductIdAsMessageKey()
    {
        var productId = Guid.NewGuid();
        var evt = CreateProductCreatedEvent(productId: productId);

        Message<string, string>? captured = null;
        _producer
            .ProduceAsync(Arg.Any<string>(),
                Arg.Do<Message<string, string>>(m => captured = m),
                Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string>());

        await _sut.PublishAsync(evt);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Key, Is.EqualTo(productId.ToString()));
    }

    [Test]
    public async Task PublishAsync_ShouldSetEventTypeAndEventIdHeaders()
    {
        var evt = CreateProductCreatedEvent();

        Message<string, string>? captured = null;
        _producer
            .ProduceAsync(Arg.Any<string>(),
                Arg.Do<Message<string, string>>(m => captured = m),
                Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string>());

        await _sut.PublishAsync(evt);

        Assert.That(captured, Is.Not.Null);
        var eventTypeHeader = captured!.Headers.FirstOrDefault(h => h.Key == "event-type");
        var eventIdHeader   = captured.Headers.FirstOrDefault(h => h.Key == "event-id");
        Assert.That(eventTypeHeader, Is.Not.Null);
        Assert.That(eventIdHeader,   Is.Not.Null);
        Assert.That(Encoding.UTF8.GetString(eventTypeHeader!.GetValueBytes()), Is.EqualTo(nameof(ProductCreatedEvent)));
        Assert.That(Encoding.UTF8.GetString(eventIdHeader!.GetValueBytes()), Is.EqualTo(evt.EventId.ToString()));
    }

    [Test]
    public async Task PublishAsync_ShouldSerializeEventWrapperWithCorrectStructure()
    {
        var sellerId   = SellerId.CreateUnique();
        var categoryId = CategoryId.CreateUnique();
        var price      = Money.Create(99.99m, "USD");
        var evt        = new ProductCreatedEvent(
            ProductId.CreateUnique(), sellerId, "Widget", "A widget",
            categoryId, price, 10, [], [], DateTime.UtcNow);

        Message<string, string>? captured = null;
        _producer
            .ProduceAsync(Arg.Any<string>(),
                Arg.Do<Message<string, string>>(m => captured = m),
                Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string>());

        await _sut.PublishAsync(evt);

        Assert.That(captured, Is.Not.Null);
        var wrapper = JsonSerializer.Deserialize<EventWrapper>(captured!.Value);
        Assert.That(wrapper, Is.Not.Null);
        Assert.That(wrapper!.EventType, Is.EqualTo(nameof(ProductCreatedEvent)));
        Assert.That(wrapper.EventId,    Is.EqualTo(evt.EventId));
        Assert.That(wrapper.Payload.GetProperty("Name").GetString(), Is.EqualTo("Widget"));
    }

    [Test]
    public async Task PublishAsync_AllKnownEventTypes_ShouldUseProductIdAsKey()
    {
        var productId = ProductId.CreateUnique();
        var now       = DateTime.UtcNow;

        IDomainEvent[] events =
        [
            new ProductUpdatedEvent(productId, "N", "D", CategoryId.CreateUnique(), Money.Create(1, "USD"), [], [], now),
            new ProductStockUpdatedEvent(productId, 10, 5, now),
            new ProductStatusChangedEvent(productId, "Draft", "Active", now),
            new ProductDeletedEvent(productId, now)
        ];

        foreach (var evt in events)
        {
            Message<string, string>? captured = null;
            _producer
                .ProduceAsync(Arg.Any<string>(),
                    Arg.Do<Message<string, string>>(m => captured = m),
                    Arg.Any<CancellationToken>())
                .Returns(new DeliveryResult<string, string>());

            await _sut.PublishAsync(evt);

            Assert.That(captured!.Key, Is.EqualTo(productId.Value.ToString()),
                $"Wrong key for {evt.GetType().Name}");
        }
    }

    [Test]
    public void PublishAsync_ShouldRethrow_WhenProducerFails()
    {
        var evt = CreateProductCreatedEvent();
        var exception = new ProduceException<string, string>(
            new Error(ErrorCode.Local_Transport), new DeliveryResult<string, string>());

        _producer
            .ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        Assert.ThrowsAsync<ProduceException<string, string>>(() => _sut.PublishAsync(evt));
    }

    #endregion

    #region PublishRawAsync

    [Test]
    public async Task PublishRawAsync_ShouldProduceToConfiguredTopic()
    {
        var eventId = Guid.NewGuid();

        await _sut.PublishRawAsync(eventId, "ProductCreatedEvent", "{}", DateTime.UtcNow, eventId.ToString());

        await _producer.Received(1).ProduceAsync(
            "product.events",
            Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishRawAsync_ShouldUseAggregateIdAsMessageKey()
    {
        var aggregateId = Guid.NewGuid().ToString();

        Message<string, string>? captured = null;
        _producer
            .ProduceAsync(Arg.Any<string>(),
                Arg.Do<Message<string, string>>(m => captured = m),
                Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string>());

        await _sut.PublishRawAsync(Guid.NewGuid(), "ProductCreatedEvent", "{}", DateTime.UtcNow, aggregateId);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Key, Is.EqualTo(aggregateId));
    }

    [Test]
    public async Task PublishRawAsync_ShouldSetEventTypeHeader()
    {
        Message<string, string>? captured = null;
        _producer
            .ProduceAsync(Arg.Any<string>(),
                Arg.Do<Message<string, string>>(m => captured = m),
                Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string>());

        await _sut.PublishRawAsync(Guid.NewGuid(), "ProductDeletedEvent", "{}", DateTime.UtcNow, "agg-1");

        Assert.That(captured, Is.Not.Null);
        var header = captured!.Headers.FirstOrDefault(h => h.Key == "event-type");
        Assert.That(header, Is.Not.Null);
        Assert.That(Encoding.UTF8.GetString(header!.GetValueBytes()), Is.EqualTo("ProductDeletedEvent"));
    }

    [Test]
    public async Task PublishRawAsync_ShouldSerializeRawContentInsideWrapper()
    {
        var content = """{"ProductId":"abc"}""";

        Message<string, string>? captured = null;
        _producer
            .ProduceAsync(Arg.Any<string>(),
                Arg.Do<Message<string, string>>(m => captured = m),
                Arg.Any<CancellationToken>())
            .Returns(new DeliveryResult<string, string>());

        await _sut.PublishRawAsync(Guid.NewGuid(), "ProductCreatedEvent", content, DateTime.UtcNow, "agg-1");

        var wrapper = JsonSerializer.Deserialize<EventWrapper>(captured!.Value);
        Assert.That(wrapper!.Payload.GetProperty("ProductId").GetString(), Is.EqualTo("abc"));
        Assert.That(wrapper.EventType, Is.EqualTo("ProductCreatedEvent"));
    }

    [Test]
    public void PublishRawAsync_ShouldRethrow_WhenProducerFails()
    {
        var exception = new ProduceException<string, string>(
            new Error(ErrorCode.Local_Transport), new DeliveryResult<string, string>());

        _producer
            .ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        Assert.ThrowsAsync<ProduceException<string, string>>(() =>
            _sut.PublishRawAsync(Guid.NewGuid(), "ProductCreatedEvent", "{}", DateTime.UtcNow, "agg-1"));
    }

    #endregion

    private static ProductCreatedEvent CreateProductCreatedEvent(Guid? productId = null) =>
        new ProductCreatedEvent(
            productId.HasValue ? ProductId.From(productId.Value) : ProductId.CreateUnique(),
            SellerId.CreateUnique(),
            "Test Product",
            "Description",
            CategoryId.CreateUnique(),
            Money.Create(10m, "USD"),
            5,
            [],
            [],
            DateTime.UtcNow);
}
