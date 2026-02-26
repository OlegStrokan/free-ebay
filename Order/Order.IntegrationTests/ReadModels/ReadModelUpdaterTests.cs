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
        var db      = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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
        var db      = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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
        var db      = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var evt = BuildOrderCreatedEvent();

        await updater.HandleAsync(evt);
        // second delivery must be silently skipped (no exception, no duplicate row)
        await updater.HandleAsync(evt);

        // exactly one row, no DB constraint violation
        var count = await db.OrderReadModels.CountAsync(o => o.Id == evt.OrderId.Value);
        count.Should().Be(1, "duplicate OrderCreatedEvent must not produce a second read model row");
    }
    
    [Fact]
    public async Task ReturnRequestReadModelUpdater_ShouldCreateReadModel_OnReturnRequestCreatedEvent()
    {
        await using var scope = _fixture.CreateScope();
        var updater = scope.ServiceProvider.GetRequiredService<ReturnRequestReadModelUpdater>();
        var db      = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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
}
