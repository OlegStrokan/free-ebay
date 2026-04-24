using System.Text.Json;
using Confluent.Kafka;
using Email.Messaging;
using Email.Models;
using Email.Options;
using Email.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.Extensions.Options.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Email.Tests.Messaging;

public class KafkaEmailConsumerTests
{
    private readonly IProcessedMessageStore _store = Substitute.For<IProcessedMessageStore>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IProducer<string, string> _producer = Substitute.For<IProducer<string, string>>();
    private readonly ILogger<KafkaEmailConsumer> _logger = Substitute.For<ILogger<KafkaEmailConsumer>>();

    private KafkaEmailConsumer Build(int maxAttempts = 3, int retryDelayMs = 0) =>
        new(Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            EmailEventsTopic = "email.events",
            EmailDlqTopic = "email.events.dlq",
            MaxDeliveryAttempts = maxAttempts,
            RetryDelayMs = retryDelayMs
        }), _store, _emailSender, _logger);

    private static (Message<string, string> message, Guid eventId) BuildEmailMessage(
        string eventType, AuthEmailMessage payload)
    {
        var eventId = Guid.NewGuid();
        var value = JsonSerializer.Serialize(new
        {
            EventId = eventId,
            EventType = eventType,
            Payload = payload,
            OccurredOn = DateTime.UtcNow
        });
        return (new Message<string, string> { Key = eventId.ToString(), Value = value, Headers = new Headers() }, eventId);
    }

    // Allows injecting a raw JSON fragment as the Payload field (e.g. "null", "\"string\"")
    private static Message<string, string> BuildMessageWithRawPayload(string eventType, string rawPayloadJson)
    {
        var eventId = Guid.NewGuid();
        var json = $"{{\"EventId\":\"{eventId}\",\"EventType\":\"{eventType}\",\"Payload\":{rawPayloadJson},\"OccurredOn\":\"{DateTime.UtcNow:O}\"}}";
        return new Message<string, string> { Key = eventId.ToString(), Value = json, Headers = new Headers() };
    }

    private static Message<string, string> BuildRawMessage(string json) =>
        new() { Key = Guid.NewGuid().ToString(), Value = json, Headers = new Headers() };

    private static AuthEmailMessage SampleEmail(bool isImportant = true) =>
        new(
            MessageId: Guid.NewGuid(),
            To: "user@example.com",
            From: "no-reply@free-ebay.com",
            Subject: "Confirm your email",
            HtmlBody: "<p>Click the link to verify</p>",
            IsImportant: isImportant,
            RequestedAtUtc: DateTime.UtcNow);
    
    [Fact]
    public async Task HandleMessageAsync_InvalidJson_PublishesToDlqAndReturnsTrue()
    {
        var message = BuildRawMessage("not-valid-json{{{");

        var result = await Build().HandleMessageAsync(message, _producer, CancellationToken.None);

        Assert.True(result);
        await _producer.Received(1).ProduceAsync(
            "email.events.dlq",
            Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_NullWrapper_PublishesToDlqAndReturnsTrue()
    {
        var message = BuildRawMessage("null");

        var result = await Build().HandleMessageAsync(message, _producer, CancellationToken.None);

        Assert.True(result);
        await _producer.Received(1).ProduceAsync(
            "email.events.dlq",
            Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("OrderConfirmationEmailRequested")]
    [InlineData("EmailVerificationRequested")]
    [InlineData("PasswordResetRequested")]
    public async Task HandleMessageAsync_InvalidPayload_PublishesToDlq(string eventType)
    {
        // a JSON string (not object) will cause Deserialize<AuthEmailMessage> to throw
        var message = BuildMessageWithRawPayload(eventType, "\"not-an-object\"");

        var result = await Build().HandleMessageAsync(message, _producer, CancellationToken.None);

        Assert.True(result);
        await _producer.Received(1).ProduceAsync(
            "email.events.dlq",
            Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_NullPayload_PublishesToDlq()
    {
        var message = BuildMessageWithRawPayload("EmailVerificationRequested", "null");

        var result = await Build().HandleMessageAsync(message, _producer, CancellationToken.None);

        Assert.True(result);
        await _producer.Received(1).ProduceAsync(
            "email.events.dlq",
            Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task HandleMessageAsync_UnsupportedEventType_SkipsWithoutDlqOrSend()
    {
        var message = BuildMessageWithRawPayload("SomeFutureEvent", "{}");

        var result = await Build().HandleMessageAsync(message, _producer, CancellationToken.None);

        Assert.True(result);
        await _producer.DidNotReceive().ProduceAsync(
            Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task HandleMessageAsync_DuplicateMessage_SkipsSendAndMarkProcessed()
    {
        var (message, _) = BuildEmailMessage("EmailVerificationRequested", SampleEmail());
        _store.IsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await Build().HandleMessageAsync(message, _producer, CancellationToken.None);

        Assert.True(result);
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().MarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
    
    [Theory]
    [InlineData("OrderConfirmationEmailRequested")]
    [InlineData("EmailVerificationRequested")]
    [InlineData("PasswordResetRequested")]
    public async Task HandleMessageAsync_ValidEmail_SendsAndMarksProcessed(string eventType)
    {
        var email = SampleEmail();
        var (message, eventId) = BuildEmailMessage(eventType, email);
        _store.IsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await Build().HandleMessageAsync(message, _producer, CancellationToken.None);

        Assert.True(result);
        await _emailSender.Received(1).SendAsync(
            email.To, email.From, email.Subject, email.HtmlBody, Arg.Any<CancellationToken>());
        await _store.Received(1).MarkProcessedAsync(eventId, Arg.Any<CancellationToken>());
        await _producer.DidNotReceive().ProduceAsync(
            Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task HandleMessageAsync_ImportantEmail_RetriesOnFailureThenSucceeds()
    {
        var (message, eventId) = BuildEmailMessage("EmailVerificationRequested", SampleEmail(isImportant: true));
        _store.IsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException(new InvalidOperationException("SMTP down")),
                Task.CompletedTask);

        var result = await Build(maxAttempts: 3, retryDelayMs: 0).HandleMessageAsync(message, _producer, CancellationToken.None);

        Assert.True(result);
        await _emailSender.Received(2).SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _store.Received(1).MarkProcessedAsync(eventId, Arg.Any<CancellationToken>());
        await _producer.DidNotReceive().ProduceAsync(
            Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_ImportantEmail_ExhaustsRetries_PublishesToDlq()
    {
        var (message, _) = BuildEmailMessage("EmailVerificationRequested", SampleEmail(isImportant: true));
        _store.IsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var result = await Build(maxAttempts: 3, retryDelayMs: 0).HandleMessageAsync(message, _producer, CancellationToken.None);

        Assert.True(result);
        await _emailSender.Received(3).SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _producer.Received(1).ProduceAsync(
            "email.events.dlq",
            Arg.Any<Message<string, string>>(),
            Arg.Any<CancellationToken>());
        await _store.DidNotReceive().MarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleMessageAsync_NonImportantEmail_FailsOnce_NoDlqNoRetry_MarksProcessed()
    {
        var (message, eventId) = BuildEmailMessage("OrderConfirmationEmailRequested", SampleEmail(isImportant: false));
        _store.IsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var result = await Build(maxAttempts: 3, retryDelayMs: 0).HandleMessageAsync(message, _producer, CancellationToken.None);

        Assert.True(result);
        // maxAttempts=1 for non-important: sent exactly once, no retry
        await _emailSender.Received(1).SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _producer.DidNotReceive().ProduceAsync(
            Arg.Any<string>(), Arg.Any<Message<string, string>>(), Arg.Any<CancellationToken>());
        // marked processed so we don't retry on next consumer restart
        await _store.Received(1).MarkProcessedAsync(eventId, Arg.Any<CancellationToken>());
    }
}
