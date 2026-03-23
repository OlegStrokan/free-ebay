using Application.Models;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Order.IntegrationTests.Infrastructure;
using Xunit;

namespace Order.IntegrationTests.Persistence;

[Collection("Integration")]
public sealed class CompensationRefundRetryRepositoryTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public CompensationRefundRetryRepositoryTests(IntegrationFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task EnqueueIfNotExistsAsync_ShouldCreatePendingRetry_WhenMissing()
    {
        var orderId = Guid.NewGuid();

        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = CreateRepository(db);

        var retry = await repository.EnqueueIfNotExistsAsync(
            orderId,
            "  PAY-123  ",
            50m,
            "usd",
            "reason",
            CancellationToken.None);

        Assert.Equal(orderId, retry.OrderId);
        Assert.Equal("PAY-123", retry.PaymentId);
        Assert.Equal("USD", retry.Currency);
        Assert.Equal(CompensationRefundRetryStatus.Pending, retry.Status);

        var rows = await db.CompensationRefundRetries
            .Where(x => x.OrderId == orderId && x.PaymentId == "PAY-123")
            .ToListAsync();

        Assert.Single(rows);
    }

    [Fact]
    public async Task EnqueueIfNotExistsAsync_ShouldReturnExistingPending_WhenDuplicate()
    {
        var orderId = Guid.NewGuid();

        await using var setupScope = _fixture.CreateScope();
        var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = CompensationRefundRetry.Create(
            orderId,
            "PAY-123",
            10m,
            "USD",
            "first",
            DateTime.UtcNow.AddMinutes(-5));

        await setupDb.CompensationRefundRetries.AddAsync(existing);
        await setupDb.SaveChangesAsync();

        await using var testScope = _fixture.CreateScope();
        var db = testScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = CreateRepository(db);

        var returned = await repository.EnqueueIfNotExistsAsync(
            orderId,
            " PAY-123 ",
            99m,
            "EUR",
            "second",
            CancellationToken.None);

        Assert.Equal(existing.Id, returned.Id);

        var rows = await db.CompensationRefundRetries
            .Where(x => x.OrderId == orderId && x.PaymentId == "PAY-123")
            .ToListAsync();

        Assert.Single(rows);
        Assert.Equal(CompensationRefundRetryStatus.Pending, rows[0].Status);
    }

    [Fact]
    public async Task EnqueueIfNotExistsAsync_ShouldAllowNewPending_WhenExistingRowIsCompleted()
    {
        var orderId = Guid.NewGuid();
        const string paymentId = "PAY-COMPLETE";

        await using var setupScope = _fixture.CreateScope();
        var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var completed = CompensationRefundRetry.Create(
            orderId,
            paymentId,
            10m,
            "USD",
            "completed",
            DateTime.UtcNow.AddMinutes(-10));
        completed.MarkCompleted(DateTime.UtcNow.AddMinutes(-9));

        await setupDb.CompensationRefundRetries.AddAsync(completed);
        await setupDb.SaveChangesAsync();

        await using var testScope = _fixture.CreateScope();
        var db = testScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = CreateRepository(db);

        var pending = await repository.EnqueueIfNotExistsAsync(
            orderId,
            paymentId,
            12m,
            "USD",
            "new pending",
            CancellationToken.None);

        Assert.Equal(CompensationRefundRetryStatus.Pending, pending.Status);

        var rows = await db.CompensationRefundRetries
            .Where(x => x.OrderId == orderId && x.PaymentId == paymentId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, x => x.Status == CompensationRefundRetryStatus.Completed);
        Assert.Contains(rows, x => x.Status == CompensationRefundRetryStatus.Pending);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistChanges_ForDetachedEntity()
    {
        var orderId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddMinutes(-3);
        CompensationRefundRetry retry;

        await using (var setupScope = _fixture.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            retry = CompensationRefundRetry.Create(
                orderId,
                "PAY-DETACHED",
                15m,
                "USD",
                "initial",
                createdAt);

            await setupDb.CompensationRefundRetries.AddAsync(retry);
            await setupDb.SaveChangesAsync();
        }

        var attemptedAt = DateTime.UtcNow;
        retry.MarkAttemptFailed("timeout", attemptedAt.AddMinutes(1), attemptedAt);

        await using (var saveScope = _fixture.CreateScope())
        {
            var saveDb = saveScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var repository = CreateRepository(saveDb);
            await repository.SaveAsync(retry, CancellationToken.None);
        }

        await using (var verifyScope = _fixture.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persisted = await verifyDb.CompensationRefundRetries
                .SingleAsync(x => x.Id == retry.Id);

            Assert.Equal(1, persisted.RetryCount);
            Assert.Equal("timeout", persisted.LastError);
            Assert.Equal(CompensationRefundRetryStatus.Pending, persisted.Status);
        }
    }

    [Fact]
    public async Task GetDuePendingAsync_ShouldReturnOnlyDuePending_InStableOrder()
    {
        // far-past clock isolates this query from rows inserted by other integration tests
        var now = DateTime.UtcNow.AddYears(-10);

        var firstDue = CompensationRefundRetry.Create(
            Guid.NewGuid(),
            "PAY-DUE-1",
            10m,
            "USD",
            "first",
            now.AddMinutes(-10));

        var secondDue = CompensationRefundRetry.Create(
            Guid.NewGuid(),
            "PAY-DUE-2",
            10m,
            "USD",
            "second",
            now.AddMinutes(-5));

        var futurePending = CompensationRefundRetry.Create(
            Guid.NewGuid(),
            "PAY-FUTURE",
            10m,
            "USD",
            "future",
            now.AddMinutes(15));

        var completed = CompensationRefundRetry.Create(
            Guid.NewGuid(),
            "PAY-COMPLETED",
            10m,
            "USD",
            "completed",
            now.AddMinutes(-20));
        completed.MarkCompleted(now.AddMinutes(-19));

        await using var setupScope = _fixture.CreateScope();
        var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await setupDb.CompensationRefundRetries.AddRangeAsync(firstDue, secondDue, futurePending, completed);
        await setupDb.SaveChangesAsync();

        await using var testScope = _fixture.CreateScope();
        var db = testScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = CreateRepository(db);

        var due = await repository.GetDuePendingAsync(now, 10, CancellationToken.None);

        Assert.Equal(2, due.Count);
        Assert.Equal(firstDue.Id, due[0].Id);
        Assert.Equal(secondDue.Id, due[1].Id);
        Assert.All(due, x => Assert.Equal(CompensationRefundRetryStatus.Pending, x.Status));
    }

    [Fact]
    public async Task UniquePendingIndex_ShouldRejectTwoPendingRows_ForSameOrderPayment()
    {
        var orderId = Guid.NewGuid();
        const string paymentId = "PAY-UNIQUE";

        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var first = CompensationRefundRetry.Create(
            orderId,
            paymentId,
            10m,
            "USD",
            "first",
            DateTime.UtcNow.AddMinutes(-2));

        var second = CompensationRefundRetry.Create(
            orderId,
            paymentId,
            11m,
            "USD",
            "second",
            DateTime.UtcNow.AddMinutes(-1));

        await db.CompensationRefundRetries.AddAsync(first);
        await db.SaveChangesAsync();

        await db.CompensationRefundRetries.AddAsync(second);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static CompensationRefundRetryRepository CreateRepository(AppDbContext db)
    {
        return new CompensationRefundRetryRepository(db, NullLogger<CompensationRefundRetryRepository>.Instance);
    }
}
