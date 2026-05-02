using System.Text.Json;
using Domain.Common;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.Events.CreateOrder;
using Domain.Interfaces;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;
using OrderAggregate = Domain.Entities.Order.Order;

namespace Order.IntegrationTests.ReadModels;

[Collection("Integration")]
public sealed class ReadModelEventDispatcherTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    private static readonly Address TestAddress = Address.Create("Baker St", "London", "UK", "NW1");

    public ReadModelEventDispatcherTests(IntegrationFixture fixture) => _fixture = fixture;
    
    /// Saves the given events to the event store, exactly as the production path does
    /// Returns the aggregate id used
    private static async Task<string> PersistEventsAsync(
        IServiceProvider sp,
        IEnumerable<IDomainEvent> events,
        string? aggregateId = null)
    {
        var id = aggregateId ?? Guid.NewGuid().ToString();
        var repo = sp.GetRequiredService<IEventStoreRepository>();
        await repo.SaveEventsAsync(id, AggregateTypes.Order, events, expectedVersion: -1);
        return id;
    }
    
    /// Returns the minimal Kafka payload that contains only the EventId,
    /// which is all ReadModelEventDispatcher needs to look up the event in the store.
    private static string BuildKafkaPayload(IDomainEvent evt) =>
        JsonSerializer.Serialize(new { EventId = evt.EventId });
    
    [Fact]
    public async Task DispatchAsync_ShouldCreateOrderReadModel_ForOrderCreatedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var sp = scope.ServiceProvider;
        var dispatcher = sp.GetRequiredService<IReadModelEventDispatcher>();
        var readDb = sp.GetRequiredService<ReadDbContext>();

        var order = OrderAggregate.Create(
            CustomerId.CreateUnique(), TestAddress,
            new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(50, "USD")) },
            "CreditCard");

        var createdEvt = order.UncommitedEvents.OfType<OrderCreatedEvent>().Single();
        var aggregateId = await PersistEventsAsync(sp, order.UncommitedEvents);

        var result = await dispatcher.DispatchAsync(
            "OrderCreatedEvent", aggregateId, BuildKafkaPayload(createdEvt), CancellationToken.None);

        result.Should().BeTrue();

        var row = await readDb.OrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == createdEvt.OrderId.Value);
        row.Should().NotBeNull("OrderReadModel should be created after dispatch");
        row!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnTrue_AndSkip_WhenEventAlreadyProcessed()
    {
        await using var scope = _fixture.CreateScope();
        var sp = scope.ServiceProvider;
        var dispatcher = sp.GetRequiredService<IReadModelEventDispatcher>();

        var order = OrderAggregate.Create(
            CustomerId.CreateUnique(), TestAddress,
            new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(50, "USD")) },
            "CreditCard");

        var createdEvt = order.UncommitedEvents.OfType<OrderCreatedEvent>().Single();
        var aggregateId = await PersistEventsAsync(sp, order.UncommitedEvents);
        var payload = BuildKafkaPayload(createdEvt);

        await dispatcher.DispatchAsync("OrderCreatedEvent", aggregateId, payload, CancellationToken.None);
        var secondResult = await dispatcher.DispatchAsync("OrderCreatedEvent", aggregateId, payload, CancellationToken.None);

        secondResult.Should().BeTrue("duplicate delivery should return true (safe to commit)");
    }

    [Fact]
    public async Task DispatchAsync_ShouldBeIdempotent_NoSecondReadModelRow()
    {
        await using var scope = _fixture.CreateScope();
        var sp = scope.ServiceProvider;
        var dispatcher = sp.GetRequiredService<IReadModelEventDispatcher>();
        var readDb = sp.GetRequiredService<ReadDbContext>();

        var order = OrderAggregate.Create(
            CustomerId.CreateUnique(), TestAddress,
            new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(50, "USD")) },
            "CreditCard");

        var createdEvt = order.UncommitedEvents.OfType<OrderCreatedEvent>().Single();
        var aggregateId = await PersistEventsAsync(sp, order.UncommitedEvents);
        var payload = BuildKafkaPayload(createdEvt);

        await dispatcher.DispatchAsync("OrderCreatedEvent", aggregateId, payload, CancellationToken.None);
        await dispatcher.DispatchAsync("OrderCreatedEvent", aggregateId, payload, CancellationToken.None);

        var count = await readDb.OrderReadModels.CountAsync(r => r.Id == createdEvt.OrderId.Value);
        count.Should().Be(1, "duplicate dispatch must produce exactly one read model row");
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnFalse_ForUnknownEventType()
    {
        await using var scope = _fixture.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IReadModelEventDispatcher>();

        var payload = JsonSerializer.Serialize(new { EventId = Guid.NewGuid() });
        var result = await dispatcher.DispatchAsync(
            "SomeUnknownEventType", Guid.NewGuid().ToString(), payload, CancellationToken.None);

        result.Should().BeFalse("unknown event types should be skipped (return false)");
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnTrue_WhenEventExistsInKafkaButNotInEventStore()
    {
        await using var scope = _fixture.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IReadModelEventDispatcher>();

        var orphanEventId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new { EventId = orphanEventId });

        var result = await dispatcher.DispatchAsync(
            "OrderCreatedEvent", Guid.NewGuid().ToString(), payload, CancellationToken.None);

        result.Should().BeTrue("missing event store entry should be a safe skip, not a blocking failure");
    }

    [Fact]
    public async Task DispatchAsync_ShouldThrow_WhenPayloadMissingEventId()
    {
        await using var scope = _fixture.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IReadModelEventDispatcher>();

        var badPayload = "{\"SomeOtherField\": \"value\"}";

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync("OrderCreatedEvent", "agg-1", badPayload, CancellationToken.None));
    }
}
