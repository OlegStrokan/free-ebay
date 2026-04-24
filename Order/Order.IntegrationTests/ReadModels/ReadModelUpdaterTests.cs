using Domain.Entities.Order;
using Domain.Events.CreateOrder;
using Domain.Events.OrderReturn;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;

namespace Order.IntegrationTests.ReadModels;

// simple tests of each eventType
[Collection("Integration")]
public sealed class ReadModelUpdaterTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public ReadModelUpdaterTests(IntegrationFixture fixture) => _fixture = fixture;
    
    private static Address TestAddress => Address.Create("Baker St", "London", "UK", "NW1");

    private static OrderCreatedEvent BuildOrderCreatedEvent(Guid? orderId = null) => new(
        OrderId.From(orderId ?? Guid.NewGuid()),
        CustomerId.CreateUnique(),
        Money.Create(100, "USD"),
        TestAddress,
        new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD")) },
        DateTime.UtcNow);

    [Fact]
    public async Task OrderReadModelUpdater_ShouldCreateReadModel_OnOrderCreatedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<OrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var evt = BuildOrderCreatedEvent();
        
        await updater.HandleAsync(evt);

        var row = await db.OrderReadModels.FirstOrDefaultAsync(o => o.Id == evt.OrderId.Value);
        row.Should().NotBeNull();
        row!.CustomerId.Should().Be(evt.CustomerId.Value);
        row.Status.Should().Be("Pending");
        row.TotalAmount.Should().Be(evt.TotalPrice.Amount);
        row.Currency.Should().Be(evt.TotalPrice.Currency);
        row.DeliveryStreet.Should().Be(TestAddress.Street);
    }

    [Fact]
    public async Task OrderReadModelUpdater_ShouldUpdateStatus_OnOrderPaidEvent()
    {
        // create read model first via OrderCreated, then send OrderPaid
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<OrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var orderId    = Guid.NewGuid();
        var customerId = CustomerId.CreateUnique();

        await updater.HandleAsync(BuildOrderCreatedEvent(orderId));

        var paidEvt = new OrderPaidEvent(
            OrderId.From(orderId),
            customerId,
            PaymentId.From("PAY-RM-001"),
            Money.Create(100, "USD"),
            DateTime.UtcNow);

        await updater.HandleAsync(paidEvt);

        var row = await db.OrderReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        row.Should().NotBeNull();
        row!.Status.Should().Be("Paid");
        row.PaymentId.Should().Be("PAY-RM-001");
    }

    [Fact]
    public async Task OrderReadModelUpdater_ShouldBeIdempotent_WhenOrderCreatedEventDeliveredTwice()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<OrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var evt = BuildOrderCreatedEvent();

        await updater.HandleAsync(evt);
        // second delivery must be silently skipped (no exception, no duplicate row)
        await updater.HandleAsync(evt);

        // exactly one row, no DB constraint violation
        var count = await db.OrderReadModels.CountAsync(o => o.Id == evt.OrderId.Value);
        count.Should().Be(1, "duplicate OrderCreatedEvent must not produce a second read model row");
    }

    [Fact]
    public async Task OrderReadModelUpdater_ShouldAssignTrackingId_OnOrderTrackingAssignedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<OrderReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var orderId = Guid.NewGuid();
        await updater.HandleAsync(BuildOrderCreatedEvent(orderId));

        var assignedAt = DateTime.UtcNow;
        await updater.HandleAsync(new OrderTrackingAssignedEvent(
            OrderId.From(orderId),
            TrackingId.From("TRACK-001"),
            assignedAt));

        var row = await db.OrderReadModels.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        row.Should().NotBeNull();
        row!.TrackingId.Should().Be("TRACK-001");
        row.UpdatedAt.Should().BeCloseTo(assignedAt, TimeSpan.FromSeconds(1));
        row.Version.Should().Be(1);
    }

    [Fact]
    public async Task OrderReadModelUpdater_ShouldUpdateStatus_OnOrderCompletedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<OrderReadModelUpdater>();
        var db = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var orderId    = Guid.NewGuid();
        var customerId = CustomerId.CreateUnique();
        await updater.HandleAsync(BuildOrderCreatedEvent(orderId));

        var completedAt = DateTime.UtcNow;
        await updater.HandleAsync(new OrderCompletedEvent(OrderId.From(orderId), customerId, completedAt));

        var row = await db.OrderReadModels.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        row.Should().NotBeNull();
        row!.Status.Should().Be("Completed");
        row.CompletedAt.Should().BeCloseTo(completedAt, TimeSpan.FromSeconds(1));
        row.UpdatedAt.Should().BeCloseTo(completedAt, TimeSpan.FromSeconds(1));
        row.Version.Should().Be(1);
    }

    [Fact]
    public async Task OrderReadModelUpdater_ShouldUpdateStatus_OnOrderCancelledEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<OrderReadModelUpdater>();
        var db = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var orderId = Guid.NewGuid();
        var customerId = CustomerId.CreateUnique();
        await updater.HandleAsync(BuildOrderCreatedEvent(orderId));

        var cancelledAt = DateTime.UtcNow;
        await updater.HandleAsync(new OrderCancelledEvent(
            OrderId.From(orderId),
            customerId,
            Reasons: new List<string> { "Customer requested" },
            cancelledAt));

        var row = await db.OrderReadModels.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        row.Should().NotBeNull();
        row!.Status.Should().Be("Cancelled");
        row.UpdatedAt.Should().BeCloseTo(cancelledAt, TimeSpan.FromSeconds(1));
        row.Version.Should().Be(1);
    }
    
    [Fact]
    public async Task ReturnRequestReadModelUpdater_ShouldCreateReadModel_OnReturnRequestCreatedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<ReturnRequestReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var productId  = ProductId.CreateUnique();
        var orderItem  = OrderItem.Create(productId, 1, Money.Create(80, "USD"));
        var rrId       = ReturnRequestId.CreateUnique();

        var evt = new ReturnRequestCreatedEvent(
            rrId,
            OrderId.CreateUnique(),
            CustomerId.CreateUnique(),
            Reason: "Item arrived damaged",
            ItemsToReturn: new List<OrderItem> { orderItem },
            RefundAmount: Money.Create(80, "USD"),
            RequestedAt: DateTime.UtcNow);

        await updater.HandleAsync(evt);

        var row = await db.ReturnRequestReadModels
            .FirstOrDefaultAsync(r => r.Id == rrId.Value);

        row.Should().NotBeNull();
        row!.OrderId.Should().Be(evt.OrderId.Value);
        row.Status.Should().Be("Pending");
        row.Reason.Should().Be("Item arrived damaged");
        row.RefundAmount.Should().Be(80);
        row.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task ReturnRequestReadModelUpdater_ShouldBeIdempotent_WhenReturnRequestCreatedEventDeliveredTwice()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<ReturnRequestReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var rrId = ReturnRequestId.CreateUnique();
        var evt = new ReturnRequestCreatedEvent(
            rrId,
            OrderId.CreateUnique(),
            CustomerId.CreateUnique(),
            Reason: "Duplicate test",
            ItemsToReturn: new List<OrderItem>(),
            RefundAmount: Money.Create(50, "USD"),
            RequestedAt: DateTime.UtcNow);

        await updater.HandleAsync(evt);
        // second delivery - must be silently skipped (no exception, no duplicate row)
        await updater.HandleAsync(evt);

        var count = await db.ReturnRequestReadModels.CountAsync(r => r.Id == rrId.Value);
        count.Should().Be(1, "duplicate ReturnRequestCreatedEvent must not produce a second read model row");
    }

    [Fact]
    public async Task ReturnRequestReadModelUpdater_ShouldUpdateStatus_OnReturnItemsReceivedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<ReturnRequestReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var rrId     = ReturnRequestId.CreateUnique();
        var orderId  = OrderId.CreateUnique();
        var custId   = CustomerId.CreateUnique();

        // seed read model via the Created event (mirrors Kafka flow)
        await updater.HandleAsync(new ReturnRequestCreatedEvent(
            rrId, orderId, custId,
            Reason: "Wrong item",
            ItemsToReturn: new List<OrderItem>(),
            RefundAmount: Money.Create(60, "USD"),
            RequestedAt: DateTime.UtcNow));

        var receivedAt = DateTime.UtcNow;
        await updater.HandleAsync(new ReturnItemsReceivedEvent(rrId, orderId, custId, receivedAt));

        var row = await db.ReturnRequestReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rrId.Value);

        row.Should().NotBeNull();
        row!.Status.Should().Be("ItemsReceived");
        row.UpdatedAt.Should().BeCloseTo(receivedAt, TimeSpan.FromSeconds(1));
        row.Version.Should().Be(1, "one status transition from Pending → ItemsReceived");
    }

    [Fact]
    public async Task ReturnRequestReadModelUpdater_ShouldUpdateStatus_OnReturnRefundProcessedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<ReturnRequestReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var rrId    = ReturnRequestId.CreateUnique();
        var orderId = OrderId.CreateUnique();
        var custId  = CustomerId.CreateUnique();

        await updater.HandleAsync(new ReturnRequestCreatedEvent(
            rrId, orderId, custId,
            Reason: "Broken",
            ItemsToReturn: new List<OrderItem>(),
            RefundAmount: Money.Create(70, "USD"),
            RequestedAt: DateTime.UtcNow));

        var refundedAt = DateTime.UtcNow;
        await updater.HandleAsync(new ReturnRefundProcessedEvent(
            rrId, orderId, custId,
            RefundId: "REF-001",
            RefundAmount: Money.Create(70, "USD"),
            RefundedAt: refundedAt));

        var row = await db.ReturnRequestReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rrId.Value);

        row.Should().NotBeNull();
        row!.Status.Should().Be("RefundProcessed");
        row.UpdatedAt.Should().BeCloseTo(refundedAt, TimeSpan.FromSeconds(1));
        row.Version.Should().Be(1);
    }

    [Fact]
    public async Task ReturnRequestReadModelUpdater_ShouldUpdateStatus_OnReturnCompletedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<ReturnRequestReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        var rrId    = ReturnRequestId.CreateUnique();
        var orderId = OrderId.CreateUnique();
        var custId  = CustomerId.CreateUnique();

        await updater.HandleAsync(new ReturnRequestCreatedEvent(
            rrId, orderId, custId,
            Reason: "Changed mind",
            ItemsToReturn: new List<OrderItem>(),
            RefundAmount: Money.Create(90, "USD"),
            RequestedAt: DateTime.UtcNow));

        var completedAt = DateTime.UtcNow;
        await updater.HandleAsync(new ReturnCompletedEvent(rrId, orderId, custId, completedAt));

        var row = await db.ReturnRequestReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rrId.Value);

        row.Should().NotBeNull();
        row!.Status.Should().Be("Completed");
        row.CompletedAt.Should().BeCloseTo(completedAt, TimeSpan.FromSeconds(1));
        row.UpdatedAt.Should().BeCloseTo(completedAt, TimeSpan.FromSeconds(1));
        row.Version.Should().Be(1);
    }
}
