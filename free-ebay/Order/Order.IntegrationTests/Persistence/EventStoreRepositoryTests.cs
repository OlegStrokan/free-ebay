using System.Text.Json;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.Events.CreateOrder;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;
// `Order` is both the parent namespace of this project and the aggregate class name.
// The alias removes the ambiguity so the compiler resolves `OrderAggregate.Create()`
// to the domain type, not to the `Order.*` namespace hierarchy.
using OrderAggregate = Domain.Entities.Order.Order;

namespace Order.IntegrationTests.Persistence;

[Collection("Integration")]
public sealed class EventStoreRepositoryTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    private static readonly Address TestAddress = Address.Create("Baker St", "London", "UK", "NW1");

    private static List<OrderItem> TestItems() =>
        new() { OrderItem.Create(ProductId.CreateUnique(), 2, Money.Create(50, "USD")) };

    public EventStoreRepositoryTests(IntegrationFixture fixture) => _fixture = fixture;
    
    [Fact]
    public async Task SaveEventsAsync_ShouldPersistEvents_AndIncrementVersion()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEventStoreRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var aggregateId = Guid.NewGuid().ToString();

        // 3 events: Created (v0), Paid (v1), Approved (v2)
        var order = OrderAggregate.Create(CustomerId.CreateUnique(), TestAddress, TestItems());
        order.Pay(PaymentId.From("PAY-1"));
        order.Approve();

        await repo.SaveEventsAsync(
            aggregateId, AggregateTypes.Order,
            order.UncommitedEvents, expectedVersion: -1,
            CancellationToken.None);

        var stored = await db.DomainEvents
            .Where(e => e.AggregateId == aggregateId && e.AggregateType == AggregateTypes.Order)
            .OrderBy(e => e.Version)
            .ToListAsync();

        stored.Should().HaveCount(3);
        stored.Select(e => e.Version).Should().Equal(0, 1, 2);
        stored.Select(e => e.AggregateId).Should().AllBe(aggregateId);
        stored.Select(e => e.AggregateType).Should().AllBe(AggregateTypes.Order);
        stored[0].EventType.Should().Be(nameof(OrderCreatedEvent));
        stored[1].EventType.Should().Be(nameof(OrderPaidEvent));
        stored[2].EventType.Should().Be(nameof(OrderApprovedEvent));
    }
    
    [Fact]
    public async Task SaveEventsAsync_ShouldThrow_OnOptimisticConcurrencyViolation()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEventStoreRepository>();

        var aggregateId = Guid.NewGuid().ToString();
        var order = OrderAggregate.Create(CustomerId.CreateUnique(), TestAddress, TestItems());

        // first writer succeeds - stream is now at version 0
        await repo.SaveEventsAsync(
            aggregateId, AggregateTypes.Order,
            order.UncommitedEvents, expectedVersion: -1,
            CancellationToken.None);

        // second writer loaded at version -1 and now tries to commit; must fail
        var act = () => repo.SaveEventsAsync(
            aggregateId, AggregateTypes.Order,
            order.UncommitedEvents, expectedVersion: -1, 
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<ConcurrencyConflictException>();
    }
    
    [Fact]
    public async Task GetEventsAsync_ShouldReturnEventsInVersionOrder_EvenIfInsertedOutOfOrder()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEventStoreRepository>();
        var db   = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var aggregateId = Guid.NewGuid().ToString();

        // serialize real events so deserialization succeeds when GetEventsAsync reads them back
        var order = OrderAggregate.Create(CustomerId.CreateUnique(), TestAddress, TestItems());
        order.Pay(PaymentId.From("PAY-2"));

        var createdJson = JsonSerializer.Serialize(
            order.UncommitedEvents[0], order.UncommitedEvents[0].GetType());
        var paidJson = JsonSerializer.Serialize(
            order.UncommitedEvents[1], order.UncommitedEvents[1].GetType());

        // insert deliberately in WRONG insertion order: Paid(v1) before Created(v0)
        db.DomainEvents.Add(DomainEvent.Create(aggregateId, AggregateTypes.Order, nameof(OrderPaidEvent),    paidJson,    version: 1, Guid.NewGuid()));
        db.DomainEvents.Add(DomainEvent.Create(aggregateId, AggregateTypes.Order, nameof(OrderCreatedEvent), createdJson, version: 0, Guid.NewGuid()));
        await db.SaveChangesAsync();

        var events = (await repo.GetEventsAsync(aggregateId, AggregateTypes.Order, CancellationToken.None)).ToList();

        events.Should().HaveCount(2);
        events[0].Should().BeOfType<OrderCreatedEvent>("version 0 must come first");
        events[1].Should().BeOfType<OrderPaidEvent>("version 1 must come second");
    }

    /* LoadOrderAsync uses this method to load only the events that occurred after
     * the snapshot version, avoiding a full replay.
     * setup: save 3 events (v0-v2) as the "historical" baseline, then save 1 more
     * event (v3) as the post-snapshot delta.
     * Assertion: GetEventsAfterVersionAsync(afterVersion: 2) returns only v3.
     */
    [Fact]
    public async Task GetEventsAfterVersionAsync_ShouldReturnOnlyDeltaEvents_AfterSnapshotVersion()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEventStoreRepository>();

        var aggregateId = Guid.NewGuid().ToString();

        // 3 events (v0 Created, v1 Paid, v2 Approved)
        var order = OrderAggregate.Create(CustomerId.CreateUnique(), TestAddress, TestItems());
        order.Pay(PaymentId.From("PAY-3"));
        order.Approve();

        await repo.SaveEventsAsync(
            aggregateId, AggregateTypes.Order,
            order.UncommitedEvents, expectedVersion: -1,
            CancellationToken.None);

        order.ClearUncommittedEvents();

        // new event (v3 Completed) after snapshot at version 2
        order.Complete();

        await repo.SaveEventsAsync(
            aggregateId, AggregateTypes.Order,
            order.UncommitedEvents, expectedVersion: 2,
            CancellationToken.None);

        // only the delta event is returned
        var delta = (await repo.GetEventsAfterVersionAsync(
            aggregateId, AggregateTypes.Order, afterVersion: 2,
            CancellationToken.None)).ToList();

        delta.Should().ContainSingle(
            "only the event with version 3 should be returned");

        delta[0].Should().BeOfType<OrderCompletedEvent>(
            "the only delta event is the OrderCompletedEvent at version 3");
    }
}
