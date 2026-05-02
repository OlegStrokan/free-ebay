using System.Text.Json;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.Interfaces;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;
using OrderAggregate = Domain.Entities.Order.Order;

namespace Order.IntegrationTests.Services;

[Collection("Integration")]
public sealed class OrderPersistenceServiceTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    private static readonly Address TestAddress = Address.Create("Baker St", "London", "UK", "NW1");

    private static List<OrderItem> TestItems() =>
        new() { OrderItem.Create(ProductId.CreateUnique(), 2, Money.Create(50, "USD")) };

    public OrderPersistenceServiceTests(IntegrationFixture fixture) => _fixture = fixture;
    
    [Fact]
    public async Task CreateOrderAsync_ShouldSaveEventsAndOutboxMessage_InSameTransaction()
    {
        await using var scope = _fixture.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IOrderPersistenceService>();
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = OrderAggregate.Create(CustomerId.CreateUnique(), TestAddress, TestItems(), "CreditCard");
        var aggregateId = order.Id.Value.ToString();

        await svc.CreateOrderAsync(order, Guid.NewGuid().ToString(), CancellationToken.None);

        var events = await db.DomainEvents
            .Where(e => e.AggregateId == aggregateId)
            .ToListAsync();

        var outbox = await db.OutboxMessages
            .Where(m => m.Id == events[0].EventId)
            .ToListAsync();

        events.Should().HaveCount(1, "only OrderCreatedEvent is raised on Create");
        outbox.Should().HaveCount(1,
            "outbox row must exist for every domain event saved");
    }
    
    [Fact]
    public async Task CreateOrderAsync_ShouldFail_WhenSameIdempotencyKeyIsUsedTwice()
    {
        await using var scope = _fixture.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IOrderPersistenceService>();

        var idempotencyKey = $"idem-{Guid.NewGuid()}";

        var order1 = OrderAggregate.Create(CustomerId.CreateUnique(), TestAddress, TestItems(), "CreditCard");
        var order2 = OrderAggregate.Create(CustomerId.CreateUnique(), TestAddress, TestItems(), "CreditCard");

        await svc.CreateOrderAsync(order1, idempotencyKey, CancellationToken.None);

        // second create with same key must violate Geneva conventions type shit
        var act = async () => await svc.CreateOrderAsync(order2, idempotencyKey, CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>(
            "IdempotencyRecords.Key is a primary key - duplicate insert must fail");
    }
    
    /* two external events arrive simultaneously for the same order (like PaymentConfirmed and FraudCheckPassed type shit)
     * both consumers load the same version and try to write => CONFLICT.
     * OR: horizontal scaling - 2 pods handle request for the same orderId at the same time: shit happens
     * retry logic: Task A hits conflict, reloads fresh state (order already paid by B),
     * action fires again but skips the operation since order is already paid => 0 events => succeeds cleanly.
     */
    [Fact]
    public async Task UpdateOrderAsync_ShouldRetryAndSucceed_OnSingleConcurrencyConflict()
    {
        await using var scopeA = _fixture.CreateScope();
        await using var scopeB = _fixture.CreateScope();

        var svcA = scopeA.ServiceProvider.GetRequiredService<IOrderPersistenceService>();
        var svcB = scopeB.ServiceProvider.GetRequiredService<IOrderPersistenceService>();

        var order = OrderAggregate.Create(CustomerId.CreateUnique(), TestAddress, TestItems(), "CreditCard");
        await svcA.CreateOrderAsync(order, Guid.NewGuid().ToString(), CancellationToken.None);
        var orderId = order.Id.Value;

        var barrier = new SemaphoreSlim(0, 1);
        var release = new SemaphoreSlim(0, 1);
        var attemptCount = 0;

        // Task A:
        //   attempt 1 - mutates the aggregate, then holds so B can commit first
        //   attempt 2 (retry) - order is already Paid by B, action is a no-op => 0 events =>
        //                       SaveEventsAsync bails early => transaction commits cleanly
        var taskA = svcA.UpdateOrderAsync(orderId, async o =>
        {
            if (Interlocked.Increment(ref attemptCount) == 1)
            {
                o.Pay(PaymentId.From("PAY-A"));
                barrier.Release();          // signal: loaded + mutated, now holding
                await release.WaitAsync();  // hold until B has committed
            }
            // retry: order already Paid - deliberately no-op so 0 events are raised
        }, CancellationToken.None);

        await barrier.WaitAsync(TimeSpan.FromSeconds(5));

        // B commits Pay(v=1) while A is frozen at v=0
        await svcB.UpdateOrderAsync(orderId, o =>
        {
            o.Pay(PaymentId.From("PAY-B"));
            return Task.CompletedTask;
        }, CancellationToken.None);

        release.Release(); // let A resume => conflict => retry → no-op => success

        // Task A must complete without throwing
        await taskA;

        attemptCount.Should().Be(2, "first attempt conflicted, second attempt succeeded");

        // DB must reflect B's commit (PAY-B), not A's stale attempt
        await using var assertScope = _fixture.CreateScope();
        var assertSvc = assertScope.ServiceProvider.GetRequiredService<IOrderPersistenceService>();
        var finalOrder = await assertSvc.LoadOrderAsync(orderId, CancellationToken.None);

        finalOrder!.Version.Should().Be(2, "OrderCreated(v=0) + B's Pay(v=1) = 2 committed events");
    }

    [Fact]
    public async Task LoadOrderAsync_ShouldRestoreFromSnapshot_AndApplyDeltaEvents()
    {
        var orderId = Guid.Empty;

        // create order => save order, build snapshot => save snapshot, add one delta - all in one scope
        await using (var scope = _fixture.CreateScope())
        {
            var svc          = scope.ServiceProvider.GetRequiredService<IOrderPersistenceService>();
            var snapshotRepo = scope.ServiceProvider.GetRequiredService<ISnapshotRepository>();

            var order = OrderAggregate.Create(CustomerId.CreateUnique(), TestAddress, TestItems(), "CreditCard");
            await svc.CreateOrderAsync(order, Guid.NewGuid().ToString(), CancellationToken.None);
            orderId = order.Id.Value;

            // capture current state as a real snapshot at v=0
            var loaded0 = await svc.LoadOrderAsync(orderId, CancellationToken.None);
            loaded0.Should().NotBeNull();

            var snapshotJson = JsonSerializer.Serialize(loaded0!.ToSnapshotState());
            await snapshotRepo.SaveAsync(
                AggregateSnapshot.Create(orderId.ToString(), AggregateTypes.Order, 0, snapshotJson),
                CancellationToken.None);

            // add one delta event so the stream is: snapshot@v=0 , event@v=1
            await svc.UpdateOrderAsync(orderId, o =>
            {
                o.Pay(PaymentId.From("PAY-SNAP"));
                return Task.CompletedTask;
            }, CancellationToken.None);
        }

        // fresh scope => no first-level EF cache - LoadOrderAsync must use snapshot + delta
        await using var assertScope = _fixture.CreateScope();
        var assertSvc = assertScope.ServiceProvider.GetRequiredService<IOrderPersistenceService>();
        var result = await assertSvc.LoadOrderAsync(orderId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Version.Should().Be(2, "OrderCreated(v=0) in snapshot + Pay delta(v=1) = 2 events total");
    }

    [Fact]
    public async Task LoadOrderAsync_ShouldFallbackToFullReplay_WhenSnapshotDeserializationFails()
    {
        var orderId = Guid.Empty;

        // create, pay, then insert a corrupt snapshot - all in one scope
        await using (var scope = _fixture.CreateScope())
        {
            var svc          = scope.ServiceProvider.GetRequiredService<IOrderPersistenceService>();
            var snapshotRepo = scope.ServiceProvider.GetRequiredService<ISnapshotRepository>();

            var order = OrderAggregate.Create(CustomerId.CreateUnique(), TestAddress, TestItems(), "CreditCard");
            await svc.CreateOrderAsync(order, Guid.NewGuid().ToString(), CancellationToken.None);
            orderId = order.Id.Value;

            await svc.UpdateOrderAsync(orderId, o =>
            {
                o.Pay(PaymentId.From("PAY-FALLBACK"));
                return Task.CompletedTask;
            }, CancellationToken.None);

            // "null" is valid JSON but deserializes to null for a record type,
            // triggering the fallback-to-full-replay path in LoadOrderAsync.
            var corruptSnapshot = AggregateSnapshot.Create(orderId.ToString(), AggregateTypes.Order, 0, "null");
            await snapshotRepo.SaveAsync(corruptSnapshot, CancellationToken.None);
        }

        // fresh scope avoids EF cache; fallback must replay all events
        await using var assertScope = _fixture.CreateScope();
        var result = await assertScope.ServiceProvider
            .GetRequiredService<IOrderPersistenceService>()
            .LoadOrderAsync(orderId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Version.Should().Be(2, "full-replay of OrderCreated(v=0) + OrderPaid(v=1) = 2 events");
    }
}
