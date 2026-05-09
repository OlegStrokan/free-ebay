using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Gateway.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Gateway.UnitTests.Services;

public sealed class KafkaUserEventPublisherTests
{
    private readonly IProducer<string, string> _producer = Substitute.For<IProducer<string, string>>();
    private readonly KafkaUserEventPublisher _publisher;

    public KafkaUserEventPublisherTests()
    {
        _publisher = new KafkaUserEventPublisher(
            _producer,
            "user.events",
            NullLogger<KafkaUserEventPublisher>.Instance);
    }

    [Fact]
    public async Task PublishAsync_ShouldProduceToCorrectTopic()
    {
        var payload = new { user_id = "user-1", catalog_item_id = "item-1" };

        await _publisher.PublishAsync("user-1", "ProductViewed", payload, CancellationToken.None);

        await _producer.Received(1).ProduceAsync(
            "user.events",
            Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldUseUserIdAsKey()
    {
        var payload = new { user_id = "user-42" };

        await _publisher.PublishAsync("user-42", "ProductClicked", payload, CancellationToken.None);

        await _producer.Received(1).ProduceAsync(
            Arg.Any<string>(),
            Arg.Is<Message<string, string>>(m => m.Key == "user-42"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldSetEventTypeHeader()
    {
        var payload = new { user_id = "user-1" };

        await _publisher.PublishAsync("user-1", "PurchaseCompleted", payload, CancellationToken.None);

        await _producer.Received(1).ProduceAsync(
            Arg.Any<string>(),
            Arg.Is<Message<string, string>>(m =>
                m.Headers != null &&
                Encoding.UTF8.GetString(m.Headers.GetLastBytes("event-type")) == "PurchaseCompleted"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ShouldSerializePayloadAsJson()
    {
        var payload = new { user_id = "user-1", catalog_item_id = "item-99", price = 42.5 };

        await _publisher.PublishAsync("user-1", "ProductViewed", payload, CancellationToken.None);

        await _producer.Received(1).ProduceAsync(
            Arg.Any<string>(),
            Arg.Is<Message<string, string>>(m =>
                m.Value.Contains("\"catalogItemId\":\"item-99\"") ||
                m.Value.Contains("\"catalog_item_id\":\"item-99\"")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Dispose_ShouldFlushAndDisposeProducer()
    {
        _publisher.Dispose();

        _producer.Received(1).Flush(Arg.Any<TimeSpan>());
        _producer.Received(1).Dispose();
    }
}
