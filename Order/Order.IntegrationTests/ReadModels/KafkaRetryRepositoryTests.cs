using Application.Interfaces;
using Application.Models;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;

namespace Order.IntegrationTests.ReadModels;

[Collection("Integration")]
public sealed class KafkaRetryRepositoryTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public KafkaRetryRepositoryTests(IntegrationFixture fixture) => _fixture = fixture;

    private static KafkaRetryRecord MakeRecord(
        string eventType = "OrderCreated",
        DateTime? nextRetryAt = null,
        string? messageKey = null) =>
        KafkaRetryRecord.Create(
            eventId: Guid.NewGuid(),
            eventType: eventType,
            topic: "order.events",
            partition: 0,
            offset: Random.Shared.NextInt64(1, long.MaxValue),
            messageKey: messageKey ?? Guid.NewGuid().ToString(),
            payload: "{\"EventId\":\"" + Guid.NewGuid() + "\"}",
            headers: null,
            correlationId: null,
            errorMessage: "test error",
            errorType: "System.Exception",
            nextRetryAt: nextRetryAt ?? DateTime.UtcNow.AddMinutes(-1));

    private static async Task<KafkaRetryRecord> LoadFromDbAsync(AppDbContext db, Guid id) =>
        await db.KafkaRetryRecords.AsNoTracking().FirstAsync(r => r.Id == id);
    
    [Fact]
    public async Task PersistAsync_ShouldSaveRecord_WithPendingStatus()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var record = MakeRecord();
        await repo.PersistAsync(record);

        var loaded = await LoadFromDbAsync(db, record.Id);
        loaded.Status.Should().Be(KafkaRetryRecordStatus.Pending);
        loaded.RetryCount.Should().Be(0);
        loaded.EventType.Should().Be("OrderCreated");
    }

    [Fact]
    public async Task PersistAsync_ShouldPreserveAllFields()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventId = Guid.NewGuid();
        var record = KafkaRetryRecord.Create(
            eventId, "OrderPaid", "order.events",
            2, 99L, "order-key", "payload-body", "{\"h\":\"v\"}",
            "trace-xyz", "error msg", "System.InvalidOperationException",
            DateTime.UtcNow.AddMinutes(3));
        await repo.PersistAsync(record);

        var loaded = await LoadFromDbAsync(db, record.Id);
        loaded.EventId.Should().Be(eventId);
        loaded.EventType.Should().Be("OrderPaid");
        loaded.Topic.Should().Be("order.events");
        loaded.Partition.Should().Be(2);
        loaded.Offset.Should().Be(99L);
        loaded.MessageKey.Should().Be("order-key");
        loaded.Payload.Should().Be("payload-body");
        loaded.CorrelationId.Should().Be("trace-xyz");
        loaded.LastErrorMessage.Should().Be("error msg");
    }
    
    [Fact]
    public async Task GetDueRecordsAsync_ShouldReturnOnlyPendingAndOverdue()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();

        var dueNow = MakeRecord(nextRetryAt: DateTime.UtcNow.AddMinutes(-5));
        var future = MakeRecord(nextRetryAt: DateTime.UtcNow.AddHours(1));
        await repo.PersistAsync(dueNow);
        await repo.PersistAsync(future);

        var results = await repo.GetDueRecordsAsync(100);

        results.Should().Contain(r => r.Id == dueNow.Id);
        results.Should().NotContain(r => r.Id == future.Id);
    }

    [Fact]
    public async Task GetDueRecordsAsync_ShouldRespectBatchSize()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();

        for (var i = 0; i < 5; i++)
            await repo.PersistAsync(MakeRecord(nextRetryAt: DateTime.UtcNow.AddMinutes(-1)));

        var results = await repo.GetDueRecordsAsync(batchSize: 2);

        // At least 2 (may be more from other tests, but batch caps at 2)
        results.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetDueRecordsAsync_ShouldNotReturn_InProgressRecords()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();

        var record = MakeRecord(nextRetryAt: DateTime.UtcNow.AddMinutes(-1));
        await repo.PersistAsync(record);
        await repo.MarkInProgressAsync(record.Id);

        var results = await repo.GetDueRecordsAsync(100);

        results.Should().NotContain(r => r.Id == record.Id);
    }
    
    [Fact]
    public async Task MarkInProgressAsync_ShouldChangeStatus_ToInProgress()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var record = MakeRecord();
        await repo.PersistAsync(record);
        await repo.MarkInProgressAsync(record.Id);

        var updated = await LoadFromDbAsync(db, record.Id);
        updated.Status.Should().Be(KafkaRetryRecordStatus.InProgress);
    }
    
    [Fact]
    public async Task MarkSucceededAsync_ShouldChangeStatus_ToSucceeded()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var record = MakeRecord();
        await repo.PersistAsync(record);
        await repo.MarkInProgressAsync(record.Id);
        await repo.MarkSucceededAsync(record.Id);

        var updated = await LoadFromDbAsync(db, record.Id);
        updated.Status.Should().Be(KafkaRetryRecordStatus.Succeeded);
    }

    [Fact]
    public async Task MarkSucceededAsync_ShouldNotAppear_InGetDueRecords()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();

        var record = MakeRecord(nextRetryAt: DateTime.UtcNow.AddMinutes(-1));
        await repo.PersistAsync(record);
        await repo.MarkInProgressAsync(record.Id);
        await repo.MarkSucceededAsync(record.Id);

        var due = await repo.GetDueRecordsAsync(100);
        due.Should().NotContain(r => r.Id == record.Id);
    }
    
    [Fact]
    public async Task RescheduleAsync_ShouldUpdateRetryCountAndNextRetryAt()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var record = MakeRecord();
        await repo.PersistAsync(record);

        var nextRetry = DateTime.UtcNow.AddMinutes(10);
        await repo.RescheduleAsync(record.Id, 2, nextRetry, "retry error", "System.Exception");

        var updated = await LoadFromDbAsync(db, record.Id);
        updated.RetryCount.Should().Be(2);
        updated.Status.Should().Be(KafkaRetryRecordStatus.Pending);
        updated.LastErrorMessage.Should().Be("retry error");
    }

    [Fact]
    public async Task RescheduleAsync_ShouldNotAppear_InGetDueRecords_WhenFutureNextRetry()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();

        var record = MakeRecord();
        await repo.PersistAsync(record);
        await repo.RescheduleAsync(record.Id, 1, DateTime.UtcNow.AddHours(1), null, null);

        var due = await repo.GetDueRecordsAsync(100);
        due.Should().NotContain(r => r.Id == record.Id);
    }
    
    [Fact]
    public async Task MarkDeadLetterAsync_ShouldChangeStatus_ToDeadLetter()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var record = MakeRecord();
        await repo.PersistAsync(record);
        await repo.MarkDeadLetterAsync(record.Id, "permanent failure", "System.Exception");

        var updated = await LoadFromDbAsync(db, record.Id);
        updated.Status.Should().Be(KafkaRetryRecordStatus.DeadLetter);
        updated.LastErrorMessage.Should().Be("permanent failure");
    }

    [Fact]
    public async Task MarkDeadLetterAsync_ShouldNotAppear_InGetDueRecords()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IKafkaRetryRepository>();

        var record = MakeRecord(nextRetryAt: DateTime.UtcNow.AddMinutes(-1));
        await repo.PersistAsync(record);
        await repo.MarkDeadLetterAsync(record.Id, "dead", null);

        var due = await repo.GetDueRecordsAsync(100);
        due.Should().NotContain(r => r.Id == record.Id);
    }
}

