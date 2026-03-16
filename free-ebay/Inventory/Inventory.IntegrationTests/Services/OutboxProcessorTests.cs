using Infrastructure.BackgroundServices;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Inventory.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Inventory.IntegrationTests.Services;

[Collection("Integration")]
public sealed class OutboxProcessorTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture fixture;

    public OutboxProcessorTests(IntegrationFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task OutboxProcessor_ShouldPublishPendingMessage_AndMarkItProcessed()
    {
        var publisher = fixture.GetPublisher();
        publisher.Reset();

        var messageId = Guid.NewGuid();
        await SeedOutboxMessageAsync(messageId, retryCount: 0);

        var processor = CreateProcessor();
        await processor.StartAsync(CancellationToken.None);

        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            var conditionMet = false;

            while (DateTime.UtcNow < deadline)
            {
                await using var scope = fixture.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                var row = await db.OutboxMessages.SingleAsync(x => x.OutboxMessageId == messageId);

                if (row.ProcessedAtUtc is not null &&
                    publisher.PublishedMessageIds.Contains(messageId))
                {
                    conditionMet = true;
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            Assert.True(conditionMet, "Condition was not met within 00:00:05.");
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task OutboxProcessor_ShouldIncrementRetryCount_WhenPublishFails()
    {
        var publisher = fixture.GetPublisher();
        publisher.Reset();
        publisher.ShouldFail = true;

        var messageId = Guid.NewGuid();
        await SeedOutboxMessageAsync(messageId, retryCount: 0);

        var processor = CreateProcessor();
        await processor.StartAsync(CancellationToken.None);

        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            var retryWasIncremented = false;

            while (DateTime.UtcNow < deadline)
            {
                await using var scope = fixture.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                var row = await db.OutboxMessages.SingleAsync(x => x.OutboxMessageId == messageId);

                if (row.RetryCount > 0)
                {
                    retryWasIncremented = true;
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            Assert.True(retryWasIncremented, "RetryCount was not incremented within 00:00:05.");

            await using var assertScope = fixture.CreateScope();
            var assertDb = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var assertRow = await assertDb.OutboxMessages.SingleAsync(x => x.OutboxMessageId == messageId);

            Assert.Null(assertRow.ProcessedAtUtc);
            Assert.Contains("Simulated publish failure", assertRow.LastError, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            publisher.ShouldFail = false;
            await processor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task OutboxProcessor_ShouldSkipMessage_WhenRetryCountReachedMaxRetries()
    {
        var publisher = fixture.GetPublisher();
        publisher.Reset();

        var messageId = Guid.NewGuid();
        await SeedOutboxMessageAsync(messageId, retryCount: 3);

        var processor = CreateProcessor(maxRetries: 3);
        await processor.StartAsync(CancellationToken.None);

        try
        {
            await Task.Delay(300);
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var row = await db.OutboxMessages.SingleAsync(x => x.OutboxMessageId == messageId);

        Assert.Equal(3, row.RetryCount);
        Assert.Null(row.ProcessedAtUtc);
        Assert.DoesNotContain(messageId, publisher.PublishedMessageIds);
    }

    private OutboxProcessor CreateProcessor(int maxRetries = 5)
    {
        var options = Options.Create(new OutboxOptions
        {
            PollIntervalMs = 50,
            BatchSize = 20,
            MaxRetries = maxRetries
        });

        var scopeFactory = fixture.Services.GetRequiredService<IServiceScopeFactory>();

        return new OutboxProcessor(
            scopeFactory,
            options,
            NullLogger<OutboxProcessor>.Instance);
    }

    private async Task SeedOutboxMessageAsync(Guid messageId, int retryCount)
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        db.OutboxMessages.Add(new OutboxMessageEntity
        {
            OutboxMessageId = messageId,
            Topic = "inventory.events",
            EventType = "InventoryReserved",
            Payload = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = retryCount,
            LastError = string.Empty
        });

        await db.SaveChangesAsync();
    }
}
