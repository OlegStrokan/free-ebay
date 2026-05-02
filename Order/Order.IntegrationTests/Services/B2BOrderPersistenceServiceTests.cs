using System.Text.Json;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.B2BOrder;
using Domain.Entities.Order;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;

namespace Order.IntegrationTests.Services;

[Collection("Integration")]
public sealed class B2BOrderPersistenceServiceTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public B2BOrderPersistenceServiceTests(IntegrationFixture fixture) => _fixture = fixture;

    private static readonly Address TestAddress = Address.Create("Wenceslas Sq 1", "Prague", "CZ", "11000");

    private static B2BOrder BuildStartedOrder() =>
        B2BOrder.Start(CustomerId.CreateUnique(), "Integration Corp", TestAddress);

    private static B2BOrder BuildOrderWithItem()
    {
        var order = BuildStartedOrder();
        order.AddItem(ProductId.CreateUnique(), 2, Money.Create(50m, "USD"));
        return order;
    }

    [Fact]
    public async Task StartB2BOrderAsync_ShouldSaveEventsAndOutboxMessage_InSameTransaction()
    {
        await using var scope = _fixture.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = BuildStartedOrder();
        var aggregateId = order.Id.Value.ToString();

        await svc.StartB2BOrderAsync(order, Guid.NewGuid().ToString(), CancellationToken.None);

        var events = await db.DomainEvents
            .Where(e => e.AggregateId == aggregateId)
            .ToListAsync();

        var outbox = await db.OutboxMessages
            .Where(m => m.Id == events[0].EventId)
            .ToListAsync();

        events.Should().HaveCount(1, "only B2BOrderStartedEvent is raised on Start");
        outbox.Should().HaveCount(1, "outbox row must exist for every domain event");
    }

    [Fact]
    public async Task StartB2BOrderAsync_ShouldFail_WhenSameIdempotencyKeyIsUsedTwice()
    {
        await using var scope = _fixture.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();

        var idempotencyKey = $"b2b-idem-{Guid.NewGuid()}";

        var order1 = BuildStartedOrder();
        var order2 = BuildStartedOrder();

        await svc.StartB2BOrderAsync(order1, idempotencyKey, CancellationToken.None);

        var act = async () => await svc.StartB2BOrderAsync(order2, idempotencyKey, CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>(
            "IdempotencyRecords.Key is a primary key - duplicate insert must fail");
    }

    /*
     * Two concurrent requests for the same B2BOrder (e.g., two UI tabs saving simultaneously).
     * Task A loads v=current, mutates (AddComment), then holds.
     * Task B commits a different mutation (adds a different comment) → v++.
     * Task A resumes → ConcurrencyConflictException → retry → no-op (0 events) → succeeds.
     */
    [Fact]
    public async Task UpdateB2BOrderAsync_ShouldRetryAndSucceed_OnSingleConcurrencyConflict()
    {
        await using var setupScope = _fixture.CreateScope();
        var setupSvc = setupScope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();

        var order = BuildStartedOrder();
        await setupSvc.StartB2BOrderAsync(order, Guid.NewGuid().ToString(), CancellationToken.None);
        var orderId = order.Id.Value;

        await using var scopeA = _fixture.CreateScope();
        await using var scopeB = _fixture.CreateScope();
        var svcA = scopeA.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();
        var svcB = scopeB.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();

        var barrier     = new SemaphoreSlim(0, 1);
        var release     = new SemaphoreSlim(0, 1);
        var attemptCount = 0;

        var taskA = svcA.UpdateB2BOrderAsync(orderId, async o =>
        {
            if (Interlocked.Increment(ref attemptCount) == 1)
            {
                o.AddComment("AuthorA", "First attempt comment");
                barrier.Release();
                await release.WaitAsync();
            }
        }, CancellationToken.None);

        await barrier.WaitAsync(TimeSpan.FromSeconds(5));

        await svcB.UpdateB2BOrderAsync(orderId, o =>
        {
            o.AddComment("AuthorB", "Concurrent comment from B");
            return Task.CompletedTask;
        }, CancellationToken.None);

        release.Release();

        await taskA;

        attemptCount.Should().Be(2, "first attempt conflicted, second attempt was a no-op");

        await using var assertScope = _fixture.CreateScope();
        var assertSvc = assertScope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();
        var final = await assertSvc.LoadB2BOrderAsync(orderId, CancellationToken.None);

        final.Should().NotBeNull();
        final!.Version.Should().Be(2, "StartedEvent + B's comment = 2 committed events");
        final.Comments.Should().Contain(c => c.Contains("AuthorB"));
    }


    [Fact]
    public async Task LoadB2BOrderAsync_ShouldRestoreFromSnapshot_AndApplyDeltaEvents()
    {
        var orderId = Guid.Empty;

        await using (var scope = _fixture.CreateScope())
        {
            var svc          = scope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();
            var snapshotRepo = scope.ServiceProvider.GetRequiredService<ISnapshotRepository>();

            var order = BuildOrderWithItem();
            await svc.StartB2BOrderAsync(order, Guid.NewGuid().ToString(), CancellationToken.None);
            orderId = order.Id.Value;

            var loaded = await svc.LoadB2BOrderAsync(orderId, CancellationToken.None);
            loaded.Should().NotBeNull();

            var snapshotJson = JsonSerializer.Serialize(loaded!.ToSnapshotState());
            await snapshotRepo.SaveAsync(
                AggregateSnapshot.Create(orderId.ToString(), AggregateTypes.B2BOrder, loaded.Version - 1, snapshotJson),
                CancellationToken.None);

            await svc.UpdateB2BOrderAsync(orderId, o =>
            {
                o.AddComment("Tester", "Delta comment after snapshot");
                return Task.CompletedTask;
            }, CancellationToken.None);
        }

        await using var assertScope = _fixture.CreateScope();
        var result = await assertScope.ServiceProvider
            .GetRequiredService<IB2BOrderPersistenceService>()
            .LoadB2BOrderAsync(orderId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Comments.Should().Contain(c => c.Contains("Delta comment after snapshot"));
        result.ActiveItems.Should().HaveCount(1, "item from snapshot must be preserved");
    }

    [Fact]
    public async Task LoadB2BOrderAsync_ShouldFallbackToFullReplay_WhenSnapshotDeserializationFails()
    {
        var orderId = Guid.Empty;

        await using (var scope = _fixture.CreateScope())
        {
            var svc          = scope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();
            var snapshotRepo = scope.ServiceProvider.GetRequiredService<ISnapshotRepository>();

            var order = BuildStartedOrder();
            await svc.StartB2BOrderAsync(order, Guid.NewGuid().ToString(), CancellationToken.None);
            orderId = order.Id.Value;

            var corruptSnapshot = AggregateSnapshot.Create(
                orderId.ToString(), AggregateTypes.B2BOrder, 0, "null");
            await snapshotRepo.SaveAsync(corruptSnapshot, CancellationToken.None);
        }

        await using var assertScope = _fixture.CreateScope();
        var result = await assertScope.ServiceProvider
            .GetRequiredService<IB2BOrderPersistenceService>()
            .LoadB2BOrderAsync(orderId, CancellationToken.None);

        result.Should().NotBeNull("full-replay of B2BOrderStartedEvent must reconstruct the aggregate");
        result!.Version.Should().Be(1, "one event (B2BOrderStartedEvent) replayed");
        result.Status.Should().Be(B2BOrderStatus.Draft);
    }

    [Fact]
    public async Task FinalizeB2BOrderAsync_ShouldSaveBothOrderAndB2BOrderEvents_AtomicallyInSingleTransaction()
    {
        await using var scope = _fixture.CreateScope();
        var b2bSvc = scope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();
        var orderSvc = scope.ServiceProvider.GetRequiredService<IOrderPersistenceService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var b2bOrder = BuildOrderWithItem();
        var b2bOrderId = b2bOrder.Id.Value;
        await b2bSvc.StartB2BOrderAsync(b2bOrder, Guid.NewGuid().ToString(), CancellationToken.None);

        var customerId = b2bOrder.CustomerId;
        var orderItems = b2bOrder.ActiveItems
            .Select(i => OrderItem.Create(i.ProductId, i.Quantity, i.EffectiveUnitPrice))
            .ToList();
        var order = Domain.Entities.Order.Order.Create(customerId, b2bOrder.DeliveryAddress, orderItems, "CreditCard");
        var idempotencyKey = Guid.NewGuid().ToString();

        await b2bSvc.FinalizeB2BOrderAsync(b2bOrderId, order, idempotencyKey, CancellationToken.None);

        var b2bOrderAggId = b2bOrderId.ToString();
        var orderAggId = order.Id.Value.ToString();

        var b2bEvents = await db.DomainEvents
            .Where(e => e.AggregateId == b2bOrderAggId)
            .ToListAsync();

        var orderEvents = await db.DomainEvents
            .Where(e => e.AggregateId == orderAggId)
            .ToListAsync();

        b2bEvents.Should().HaveCountGreaterThan(1, "B2BOrder should have StartedEvent + FinalizedEvent");
        orderEvents.Should().HaveCountGreaterThan(0, "Order should have CreatedEvent");
    }

    [Fact]
    public async Task FinalizeB2BOrderAsync_ShouldUpdateB2BOrderStatus_ToFinalized()
    {
        await using var scope = _fixture.CreateScope();
        var b2bSvc = scope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();

        var b2bOrder = BuildOrderWithItem();
        var b2bOrderId = b2bOrder.Id.Value;
        await b2bSvc.StartB2BOrderAsync(b2bOrder, Guid.NewGuid().ToString(), CancellationToken.None);

        var beforeFinalize = await b2bSvc.LoadB2BOrderAsync(b2bOrderId, CancellationToken.None);
        beforeFinalize!.Status.Should().Be(B2BOrderStatus.Draft);
        beforeFinalize.FinalizedOrderId.Should().BeNull();

        var orderItems = b2bOrder.ActiveItems
            .Select(i => OrderItem.Create(i.ProductId, i.Quantity, i.EffectiveUnitPrice))
            .ToList();
        var order = Domain.Entities.Order.Order.Create(b2bOrder.CustomerId, b2bOrder.DeliveryAddress, orderItems, "CreditCard");

        await b2bSvc.FinalizeB2BOrderAsync(b2bOrderId, order, Guid.NewGuid().ToString(), CancellationToken.None);

        var afterFinalize = await b2bSvc.LoadB2BOrderAsync(b2bOrderId, CancellationToken.None);
        afterFinalize.Should().NotBeNull();
        afterFinalize!.Status.Should().Be(B2BOrderStatus.Finalized);
        afterFinalize.FinalizedOrderId.Should().Be(order.Id.Value);
    }

    [Fact]
    public async Task FinalizeB2BOrderAsync_ShouldCreateOutboxMessagesForBothAggregates()
    {
        await using var scope = _fixture.CreateScope();
        var b2bSvc = scope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var b2bOrder = BuildOrderWithItem();
        var b2bOrderId = b2bOrder.Id.Value;
        await b2bSvc.StartB2BOrderAsync(b2bOrder, Guid.NewGuid().ToString(), CancellationToken.None);

        var orderItems = b2bOrder.ActiveItems
            .Select(i => OrderItem.Create(i.ProductId, i.Quantity, i.EffectiveUnitPrice))
            .ToList();
        var order = Domain.Entities.Order.Order.Create(b2bOrder.CustomerId, b2bOrder.DeliveryAddress, orderItems, "CreditCard");

        await b2bSvc.FinalizeB2BOrderAsync(b2bOrderId, order, Guid.NewGuid().ToString(), CancellationToken.None);

        var b2bOrderAggId = b2bOrderId.ToString();
        var orderAggId = order.Id.Value.ToString();

        var allOutbox = await db.OutboxMessages.ToListAsync();
        var b2bOutbox = allOutbox.Where(m => m.AggregateId == b2bOrderAggId).ToList();
        var orderOutbox = allOutbox.Where(m => m.AggregateId == orderAggId).ToList();

        b2bOutbox.Should().HaveCountGreaterThan(0, "B2BOrder events should be in outbox");
        orderOutbox.Should().HaveCountGreaterThan(0, "Order events should be in outbox");
    }

    [Fact]
    public async Task FinalizeB2BOrderAsync_ShouldSaveIdempotencyRecord()
    {
        await using var scope = _fixture.CreateScope();
        var b2bSvc = scope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var b2bOrder = BuildOrderWithItem();
        var b2bOrderId = b2bOrder.Id.Value;
        await b2bSvc.StartB2BOrderAsync(b2bOrder, Guid.NewGuid().ToString(), CancellationToken.None);

        var orderItems = b2bOrder.ActiveItems
            .Select(i => OrderItem.Create(i.ProductId, i.Quantity, i.EffectiveUnitPrice))
            .ToList();
        var order = Domain.Entities.Order.Order.Create(b2bOrder.CustomerId, b2bOrder.DeliveryAddress, orderItems, "CreditCard");
        var idempotencyKey = $"finalize-{Guid.NewGuid()}";

        await b2bSvc.FinalizeB2BOrderAsync(b2bOrderId, order, idempotencyKey, CancellationToken.None);

        var record = await db.IdempotencyRecords
            .SingleOrDefaultAsync(r => r.Key == idempotencyKey);

        record.Should().NotBeNull();
        record!.ResultId.Should().Be(order.Id.Value);
    }

    [Fact]
    public async Task FinalizeB2BOrderAsync_ShouldFail_WhenB2BOrderNotFound()
    {
        await using var scope = _fixture.CreateScope();
        var b2bSvc = scope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();

        var fakeBb2OrderId = Guid.NewGuid();
        var productId = ProductId.CreateUnique();
        var item = OrderItem.Create(productId, 2, Money.Create(100, "USD"));
        var orderItems  = new List<OrderItem> { item };
        
        var order = Domain.Entities.Order.Order.Create(
            CustomerId.CreateUnique(),
            TestAddress,
            orderItems,
            "CreditCard");

        var act = async () => await b2bSvc.FinalizeB2BOrderAsync(
            fakeBb2OrderId,
            order,
            Guid.NewGuid().ToString(),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"*not found*");
    }

    [Fact]
    public async Task FinalizeB2BOrderAsync_ShouldIdempotencyCheckOnSecondCall_ReturnSameOrderId()
    {
        await using var scope = _fixture.CreateScope();
        var b2bSvc = scope.ServiceProvider.GetRequiredService<IB2BOrderPersistenceService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var b2bOrder = BuildOrderWithItem();
        var b2bOrderId = b2bOrder.Id.Value;
        await b2bSvc.StartB2BOrderAsync(b2bOrder, Guid.NewGuid().ToString(), CancellationToken.None);

        var idempotencyKey = $"finalize-dup-{Guid.NewGuid()}";

        var orderItems = b2bOrder.ActiveItems
            .Select(i => OrderItem.Create(i.ProductId, i.Quantity, i.EffectiveUnitPrice))
            .ToList();
        var order1 = Domain.Entities.Order.Order.Create(b2bOrder.CustomerId, b2bOrder.DeliveryAddress, orderItems, "CreditCard");
        var orderId1 = order1.Id.Value;

        await b2bSvc.FinalizeB2BOrderAsync(b2bOrderId, order1, idempotencyKey, CancellationToken.None);

        var record = await db.IdempotencyRecords
            .SingleOrDefaultAsync(r => r.Key == idempotencyKey);
        record.Should().NotBeNull();
        record!.ResultId.Should().Be(orderId1);

        var orderItems2 = b2bOrder.ActiveItems
            .Select(i => OrderItem.Create(i.ProductId, i.Quantity, i.EffectiveUnitPrice))
            .ToList();
        var order2 = Domain.Entities.Order.Order.Create(b2bOrder.CustomerId, b2bOrder.DeliveryAddress, orderItems2, "CreditCard");

        await b2bSvc.FinalizeB2BOrderAsync(b2bOrderId, order2, idempotencyKey, CancellationToken.None);

        var recordAfter = await db.IdempotencyRecords
            .SingleOrDefaultAsync(r => r.Key == idempotencyKey);
        recordAfter!.ResultId.Should().Be(orderId1, "idempotency key should return same result");
    }
}
