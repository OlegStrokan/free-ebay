using Application.Interfaces;
using Application.Models;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Product.IntegrationTests.Infrastructure;
using Xunit;

namespace Product.IntegrationTests.Persistence;

[Collection("Integration")]
public sealed class OutboxRepositoryTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public OutboxRepositoryTests(IntegrationFixture fixture) => _fixture = fixture;

    private static OutboxMessage BuildMessage(Guid? id = null)
    {
        var msgId = id ?? Guid.NewGuid();
        return new OutboxMessage(msgId, "ProductCreatedEvent", "{}", DateTime.UtcNow, msgId.ToString());
    }

    [Fact]
    public async Task AddAsync_ShouldPersistOutboxMessage()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var messageId  = Guid.NewGuid();
        var aggregateId = Guid.NewGuid().ToString();

        await repo.AddAsync(messageId, "ProductCreatedEvent", "{}", DateTime.UtcNow, aggregateId);
        await db.SaveChangesAsync();

        var stored = await db.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId);

        stored.Should().NotBeNull();
        stored!.Type.Should().Be("ProductCreatedEvent");
        stored.AggregateId.Should().Be(aggregateId);
        stored.ProcessedOn.Should().BeNull("new messages are unprocessed");
        stored.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUnprocessedMessagesAsync_ShouldOnlyReturnMessagesWithNullProcessedOn()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var unprocessedId = Guid.NewGuid();
        var processedId   = Guid.NewGuid();

        var unprocessed = new OutboxMessage(unprocessedId, "EventA", "{}", DateTime.UtcNow, unprocessedId.ToString());
        var processed   = new OutboxMessage(processedId,   "EventB", "{}", DateTime.UtcNow, processedId.ToString());
        processed.MarkAsProcessed();

        db.OutboxMessages.AddRange(unprocessed, processed);
        await db.SaveChangesAsync();

        var results = await repo.GetUnprocessedMessagesAsync(100, maxRetries: 5);

        results.Should().Contain(m => m.Id == unprocessedId,
            "unprocessed messages must be returned");
        results.Should().NotContain(m => m.Id == processedId,
            "already-processed messages must be excluded");
    }

    [Fact]
    public async Task GetUnprocessedMessagesAsync_ShouldExcludeMessagesAtOrAboveMaxRetries()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var okId       = Guid.NewGuid();
        var exhaustedId = Guid.NewGuid();

        var okMsg = new OutboxMessage(okId, "EventOk", "{}", DateTime.UtcNow, okId.ToString());
        var exhaustedMsg = new OutboxMessage(exhaustedId, "EventExhausted", "{}", DateTime.UtcNow, exhaustedId.ToString());

        db.OutboxMessages.AddRange(okMsg, exhaustedMsg);
        await db.SaveChangesAsync();

        // Simulate 5 retries on the exhausted message
        await db.OutboxMessages
            .Where(m => m.Id == exhaustedId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.RetryCount, 5));

        // maxRetries = 5, so messages with RetryCount >= 5 are excluded
        var results = await repo.GetUnprocessedMessagesAsync(100, maxRetries: 5);

        results.Should().Contain(m => m.Id == okId, "non-exhausted messages must be returned");
        results.Should().NotContain(m => m.Id == exhaustedId, "exhausted messages must be excluded from processing");
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldSetProcessedOn()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var messageId = Guid.NewGuid();
        db.OutboxMessages.Add(new OutboxMessage(messageId, "SomeEvent", "{}", DateTime.UtcNow, messageId.ToString()));
        await db.SaveChangesAsync();

        await repo.MarkAsProcessedAsync(messageId);

        db.ChangeTracker.Clear();
        var row = await db.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId);

        row!.ProcessedOn.Should().NotBeNull("MarkAsProcessedAsync must set ProcessedOn");
    }

    [Fact]
    public async Task IncrementRetryCountAsync_ShouldBumpCountAndRecordError()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var messageId = Guid.NewGuid();
        db.OutboxMessages.Add(new OutboxMessage(messageId, "SomeEvent", "{}", DateTime.UtcNow, messageId.ToString()));
        await db.SaveChangesAsync();

        await repo.IncrementRetryCountAsync(messageId, "broker timeout");

        db.ChangeTracker.Clear();
        var row = await db.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId);

        row!.RetryCount.Should().Be(1);
        row.Error.Should().Be("broker timeout");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveMessage()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var messageId = Guid.NewGuid();
        db.OutboxMessages.Add(new OutboxMessage(messageId, "SomeEvent", "{}", DateTime.UtcNow, messageId.ToString()));
        await db.SaveChangesAsync();

        await repo.DeleteAsync(messageId);

        var row = await db.OutboxMessages.FirstOrDefaultAsync(m => m.Id == messageId);
        row.Should().BeNull("DeleteAsync must remove the row");
    }

    [Fact]
    public async Task DeleteProcessedMessagesAsync_ShouldDeleteOldProcessedMessages_ButRetainRecent()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        var oldId    = Guid.NewGuid();
        var recentId = Guid.NewGuid();

        var oldMsg = new OutboxMessage(oldId, "OldEvent", "{}", DateTime.UtcNow.AddDays(-10), oldId.ToString());
        // simulate old processing time via reflection so the cutoff query triggers
        oldMsg.MarkAsProcessed();

        var recentMsg = new OutboxMessage(recentId, "NewEvent", "{}", DateTime.UtcNow, recentId.ToString());
        recentMsg.MarkAsProcessed();

        db.OutboxMessages.AddRange(oldMsg, recentMsg);
        await db.SaveChangesAsync();

        // override ProcessedOn to 10 days ago for oldMsg using EF raw update
        await db.OutboxMessages
            .Where(m => m.Id == oldId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ProcessedOn, DateTime.UtcNow.AddDays(-10)));

        await repo.DeleteProcessedMessagesAsync();

        var oldRow    = await db.OutboxMessages.FirstOrDefaultAsync(m => m.Id == oldId);
        var recentRow = await db.OutboxMessages.FirstOrDefaultAsync(m => m.Id == recentId);

        oldRow.Should().BeNull("messages processed more than 7 days ago must be deleted");
        recentRow.Should().NotBeNull("recently processed messages must be retained");
    }
}
