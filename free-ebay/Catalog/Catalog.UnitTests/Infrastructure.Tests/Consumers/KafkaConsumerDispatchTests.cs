using System.Text.Json;
using Application.Consumers;
using Infrastructure.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Infrastructure.Tests.Consumers;

[TestFixture]
public class KafkaConsumerDispatchTests
{
    private IServiceProvider _serviceProvider;
    private IServiceScope _serviceScope;
    private IServiceScopeFactory _scopeFactory;
    private ILogger<KafkaConsumerBackgroundService> _logger;
    private KafkaConsumerBackgroundService _sut;

    [SetUp]
    public void SetUp()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceScope = Substitute.For<IServiceScope>();
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _logger = Substitute.For<ILogger<KafkaConsumerBackgroundService>>();

        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);
        _scopeFactory.CreateScope().Returns(_serviceScope);

        var options = Options.Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            ConsumerGroupId = "test-group",
            ProductEventsTopic = "product.events",
        });

        _sut = new KafkaConsumerBackgroundService(options, _serviceProvider, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceScope.Dispose();
        _sut.Dispose();
    }

    [Test]
    public async Task DispatchAsync_ShouldCallMatchingConsumer()
    {
        var consumer = Substitute.For<IProductEventConsumer>();
        consumer.EventType.Returns("ProductCreatedEvent");
        SetupConsumers(consumer);

        await _sut.DispatchAsync("ProductCreatedEvent", EmptyPayload(), CancellationToken.None);

        await consumer.Received(1).ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_ShouldForwardPayloadToConsumer()
    {
        var consumer = Substitute.For<IProductEventConsumer>();
        consumer.EventType.Returns("ProductDeletedEvent");
        SetupConsumers(consumer);

        var payload = JsonDocument.Parse("""{"ProductId":{"Value":"abc"}}""").RootElement;

        JsonElement captured = default;
        consumer.When(x => x.ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.ArgAt<JsonElement>(0));

        await _sut.DispatchAsync("ProductDeletedEvent", payload, CancellationToken.None);

        Assert.That(captured.GetRawText(), Is.EqualTo(payload.GetRawText()));
    }

    [Test]
    public async Task DispatchAsync_WhenNoConsumerRegistered_ShouldNotThrow()
    {
        SetupConsumers(); // no consumers

        Assert.DoesNotThrowAsync(() =>
            _sut.DispatchAsync("UnknownEvent", EmptyPayload(), CancellationToken.None));
    }

    [Test]
    public async Task DispatchAsync_WhenNoConsumerRegistered_ShouldNotCallAnyConsumer()
    {
        var consumer = Substitute.For<IProductEventConsumer>();
        consumer.EventType.Returns("ProductCreatedEvent");
        SetupConsumers(consumer);

        await _sut.DispatchAsync("UnknownEvent", EmptyPayload(), CancellationToken.None);

        await consumer.DidNotReceiveWithAnyArgs().ConsumeAsync(default, default);
    }

    [Test]
    public async Task DispatchAsync_WithMultipleConsumers_ShouldOnlyCallMatchingOne()
    {
        var createdConsumer = Substitute.For<IProductEventConsumer>();
        createdConsumer.EventType.Returns("ProductCreatedEvent");

        var deletedConsumer = Substitute.For<IProductEventConsumer>();
        deletedConsumer.EventType.Returns("ProductDeletedEvent");

        SetupConsumers(createdConsumer, deletedConsumer);

        await _sut.DispatchAsync("ProductDeletedEvent", EmptyPayload(), CancellationToken.None);

        await deletedConsumer.Received(1).ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>());
        await createdConsumer.DidNotReceiveWithAnyArgs().ConsumeAsync(default, default);
    }

    [TestCase("ProductCreatedEvent")]
    [TestCase("ProductUpdatedEvent")]
    [TestCase("ProductStockUpdatedEvent")]
    [TestCase("ProductStatusChangedEvent")]
    [TestCase("ProductDeletedEvent")]
    public async Task DispatchAsync_ShouldRouteAllKnownEventTypes(string eventType)
    {
        var consumer = Substitute.For<IProductEventConsumer>();
        consumer.EventType.Returns(eventType);
        SetupConsumers(consumer);

        await _sut.DispatchAsync(eventType, EmptyPayload(), CancellationToken.None);

        await consumer.Received(1).ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void DispatchAsync_WhenConsumerThrows_ShouldPropagateException()
    {
        var consumer = Substitute.For<IProductEventConsumer>();
        consumer.EventType.Returns("ProductCreatedEvent");
        consumer
            .ConsumeAsync(Arg.Any<JsonElement>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("indexing failed")));
        SetupConsumers(consumer);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.DispatchAsync("ProductCreatedEvent", EmptyPayload(), CancellationToken.None));
    }

    private void SetupConsumers(params IProductEventConsumer[] consumers)
    {
        _serviceScope.ServiceProvider
            .GetService(typeof(IEnumerable<IProductEventConsumer>))
            .Returns(consumers.AsEnumerable());
    }

    private static JsonElement EmptyPayload() =>
        JsonDocument.Parse("{}").RootElement;
}
