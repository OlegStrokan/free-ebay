using System.Text;
using Confluent.Kafka;
using Email.Messaging;
using Email.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Extensions.Options.Options;
using NSubstitute;

namespace Email.Tests.Messaging;

public class KafkaEmailDlqReplayWorkerTests
{
    private readonly IProducer<string, string> _producer = Substitute.For<IProducer<string, string>>();
    private readonly ILogger<KafkaEmailDlqReplayWorker> _logger = Substitute.For<ILogger<KafkaEmailDlqReplayWorker>>();

    private KafkaEmailDlqReplayWorker Build(int maxReplayAttempts = 5, int baseDelayMs = 0) =>
        new(Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            EmailEventsTopic = "email.events",
            EmailDlqTopic = "email.events.dlq",
            EnableDlqReplay = true,
            MaxDlqReplayAttempts = maxReplayAttempts,
            DlqReplayBaseDelayMs = baseDelayMs
        }), _logger);

    private static Message<string, string> BuildDlqMessage(int replayAttempt)
    {
        var headers = new Headers();
        headers.Add("dlq-replay-attempt", Encoding.UTF8.GetBytes(replayAttempt.ToString()));
        return new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = $"{{\"EventId\":\"{Guid.NewGuid()}\",\"EventType\":\"EmailVerificationRequested\",\"Payload\":{{}},\"OccurredOn\":\"{DateTime.UtcNow:O}\"}}",
            Headers = headers
        };
    }

    [Fact]
    public async Task ReplayMessageAsync_WhenAttemptEqualsMax_SkipsWithoutPublishing()
    {
        var message = BuildDlqMessage(replayAttempt: 5);

        await Build(maxReplayAttempts: 5).ReplayMessageAsync(message, _producer, CancellationToken.None);

        await _producer.DidNotReceive().ProduceAsync(
            Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayMessageAsync_WhenAttemptExceedsMax_SkipsWithoutPublishing()
    {
        var message = BuildDlqMessage(replayAttempt: 10);

        await Build(maxReplayAttempts: 5).ReplayMessageAsync(message, _producer, CancellationToken.None);

        await _producer.DidNotReceive().ProduceAsync(
            Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayMessageAsync_WhenBelowMax_ProducesToEmailEventsTopic()
    {
        var message = BuildDlqMessage(replayAttempt: 2);

        await Build(maxReplayAttempts: 5).ReplayMessageAsync(message, _producer, CancellationToken.None);

        await _producer.Received(1).ProduceAsync(
            "email.events",
            Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayMessageAsync_IncrementsDlqReplayAttemptHeader()
    {
        var message = BuildDlqMessage(replayAttempt: 2);
        Message<string, string>? captured = null;
        _producer
            .ProduceAsync(
                Arg.Any<string>(),
                Arg.Do<Message<string, string>>(m => captured = m),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeliveryResult<string, string>>(null!));

        await Build(maxReplayAttempts: 5).ReplayMessageAsync(message, _producer, CancellationToken.None);

        Assert.NotNull(captured);
        var attemptHeader = captured.Headers.FirstOrDefault(h => h.Key == "dlq-replay-attempt");
        Assert.NotNull(attemptHeader);
        Assert.Equal("3", Encoding.UTF8.GetString(attemptHeader.GetValueBytes()));
    }

    [Fact]
    public async Task ReplayMessageAsync_SetsDlqReplayedAtHeader()
    {
        var message = BuildDlqMessage(replayAttempt: 0);
        Message<string, string>? captured = null;
        _producer
            .ProduceAsync(
                Arg.Any<string>(),
                Arg.Do<Message<string, string>>(m => captured = m),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeliveryResult<string, string>>(null!));

        var before = DateTime.UtcNow;
        await Build().ReplayMessageAsync(message, _producer, CancellationToken.None);

        Assert.NotNull(captured);
        var replayedAtHeader = captured.Headers.FirstOrDefault(h => h.Key == "dlq-replayed-at");
        Assert.NotNull(replayedAtHeader);
        var replayedAt = DateTime.Parse(Encoding.UTF8.GetString(replayedAtHeader.GetValueBytes())).ToUniversalTime();
        Assert.True(replayedAt >= before);
    }

    [Fact]
    public async Task ReplayMessageAsync_PreservesOriginalMessageKeyAndValue()
    {
        var message = BuildDlqMessage(replayAttempt: 1);
        Message<string, string>? captured = null;
        _producer
            .ProduceAsync(
                Arg.Any<string>(),
                Arg.Do<Message<string, string>>(m => captured = m),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeliveryResult<string, string>>(null!));

        await Build().ReplayMessageAsync(message, _producer, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(message.Key, captured.Key);
        Assert.Equal(message.Value, captured.Value);
    }

    [Fact]
    public async Task ReplayMessageAsync_NoReplayAttemptHeader_TreatsAsFirstAttempt()
    {
        // message with no dlq-replay-attempt header → treated as attempt 0
        var message = new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = "{}",
            Headers = new Headers()
        };
        Message<string, string>? captured = null;
        _producer
            .ProduceAsync(
                Arg.Any<string>(),
                Arg.Do<Message<string, string>>(m => captured = m),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeliveryResult<string, string>>(null!));

        await Build(maxReplayAttempts: 5).ReplayMessageAsync(message, _producer, CancellationToken.None);

        Assert.NotNull(captured);
        var attemptHeader = captured.Headers.FirstOrDefault(h => h.Key == "dlq-replay-attempt");
        Assert.NotNull(attemptHeader);
        Assert.Equal("1", Encoding.UTF8.GetString(attemptHeader.GetValueBytes()));
    }
}
