using Application.Interfaces;
using Application.Models;
using FluentAssertions;
using Infrastructure.BackgroundServices;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Product.IntegrationTests.Infrastructure;
using Product.IntegrationTests.TestHelpers;
using Xunit;

namespace Product.IntegrationTests.Services;

// Kafka is mocked via FakeEventPublisher
[Collection("Integration")]
public sealed class OutboxProcessorTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public OutboxProcessorTests(IntegrationFixture fixture) => _fixture = fixture;

    private (OutboxProcessor Processor, FakeEventPublisher Publisher) CreateProcessor(int maxRetries = 5)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:PollIntervalMs"] = "50",
                ["Outbox:BatchSize"]      = "20",
                ["Outbox:MaxRetries"]     = maxRetries.ToString(),
                ["Outbox:MaxParallelism"] = "1"
            })
            .Build();

        var publisher = new FakeEventPublisher();

        var processor = new OutboxProcessor(
            _fixture.Services,
            publisher,
            NullLogger<OutboxProcessor>.Instance,
            config);

        return (processor, publisher);
    }

    [Fact]
    public async Task OutboxProcessor_ShouldPublishPendingMessage_AndMarkItProcessed()
    {
        // seed one unprocessed outbox message before starting the processor
        var messageId   = Guid.NewGuid();
        var aggregateId = messageId.ToString();

        await using (var setupScope = _fixture.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<ProductDbContext>();
            db.OutboxMessages.Add(new OutboxMessage(messageId, "ProductCreatedEvent", "{}", DateTime.UtcNow, aggregateId));
            await db.SaveChangesAsync();
        }

        var (processor, publisher) = CreateProcessor();

        await processor.StartAsync(CancellationToken.None);
        await Task.Delay(400);
        await processor.StopAsync(CancellationToken.None);

        publisher.Published.Should()
            .Contain(p => p.Id == messageId,
                "the processor must deliver every unprocessed outbox message to IEventPublisher");

        await using var assertScope = _fixture.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var row = await assertDb.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId);
        row.Should().NotBeNull();
        row!.ProcessedOn.Should().NotBeNull("MarkAsProcessedAsync must set ProcessedOn after publishing");
    }

    [Fact]
    public async Task OutboxProcessor_ShouldNotPublishAlreadyProcessedMessage()
    {
        var messageId = Guid.NewGuid();

        await using (var setupScope = _fixture.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<ProductDbContext>();

            var msg = new OutboxMessage(messageId, "ProductStatusChangedEvent", "{}", DateTime.UtcNow, messageId.ToString());
            msg.MarkAsProcessed();
            db.OutboxMessages.Add(msg);
            await db.SaveChangesAsync();
        }

        var (processor, publisher) = CreateProcessor();

        await processor.StartAsync(CancellationToken.None);
        await Task.Delay(400);
        await processor.StopAsync(CancellationToken.None);

        publisher.Published.Should().NotContain(p => p.Id == messageId,
            "GetUnprocessedMessagesAsync filters rows with ProcessedOn != null");
    }

    [Fact]
    public async Task OutboxProcessor_ShouldMarkAsProcessed_WhenRetryLimitReached()
    {
        const int maxRetries = 3;
        var messageId = Guid.NewGuid();

        await using (var setupScope = _fixture.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<ProductDbContext>();

            var msg = new OutboxMessage(messageId, "ProductCreatedEvent", "{}", DateTime.UtcNow, messageId.ToString());
            for (var i = 0; i < maxRetries; i++)
                msg.IncrementRetry($"error #{i + 1}");

            db.OutboxMessages.Add(msg);
            await db.SaveChangesAsync();
        }

        var (processor, publisher) = CreateProcessor(maxRetries: maxRetries);

        await processor.StartAsync(CancellationToken.None);
        await Task.Delay(400);
        await processor.StopAsync(CancellationToken.None);

        // the message is marked processed (not published) once retry limit is hit
        await using var assertScope = _fixture.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var row = await assertDb.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId);
        row!.ProcessedOn.Should().NotBeNull(
            "processor marks the message as processed when max retries are exceeded to stop infinite retry");

        publisher.Published.Should().NotContain(p => p.Id == messageId,
            "a message past its retry limit must not be published");
    }

    [Fact]
    public async Task OutboxProcessor_ShouldIncrementRetryCount_WhenPublishFails()
    {
        var messageId = Guid.NewGuid();

        await using (var setupScope = _fixture.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<ProductDbContext>();
            db.OutboxMessages.Add(new OutboxMessage(messageId, "ProductCreatedEvent", "{}", DateTime.UtcNow, messageId.ToString()));
            await db.SaveChangesAsync();
        }

        // Use a high retry ceiling so the test's 400ms window (≈8 poll cycles at 50ms)
        // cannot exhaust the limit and mark the message as processed prematurely.
        var (processor, publisher) = CreateProcessor(maxRetries: 100);
        publisher.ShouldFail = true; // simulate broker outage

        await processor.StartAsync(CancellationToken.None);
        await Task.Delay(400);
        await processor.StopAsync(CancellationToken.None);

        await using var assertScope = _fixture.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var row = await assertDb.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId);
        row!.RetryCount.Should().BeGreaterThan(0,
            "IncrementRetryCountAsync must be called when publish throws");
        row.ProcessedOn.Should().BeNull("the message has not been successfully delivered yet");
    }
}
