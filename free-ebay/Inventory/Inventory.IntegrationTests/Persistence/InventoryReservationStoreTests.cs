using Application.Interfaces;
using Application.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Inventory.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.IntegrationTests.Persistence;

[Collection("Integration")]
public sealed class InventoryReservationStoreTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture fixture;

    public InventoryReservationStoreTests(IntegrationFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ReserveAsync_ShouldReserveStock_AndPersistReservationMovementAndOutbox()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await SeedStockAsync(productId, availableQuantity: 10);

        await using (var scope = fixture.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IInventoryReservationStore>();
            var reserveResult = await store.ReserveAsync(
                orderId,
                [new ReserveStockItem(productId, 3)],
                CancellationToken.None);

            Assert.True(reserveResult.Success);
            Assert.False(reserveResult.IsIdempotentReplay);
            Assert.True(Guid.TryParse(reserveResult.ReservationId, out _));
        }

        await using var assertScope = fixture.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var stock = await db.ProductStocks.SingleAsync(x => x.ProductId == productId);
        Assert.Equal(7, stock.AvailableQuantity);
        Assert.Equal(3, stock.ReservedQuantity);

        var reservation = await db.InventoryReservations
            .Include(x => x.Items)
            .SingleAsync(x => x.OrderId == orderId);

        Assert.Equal("Active", reservation.Status);
        Assert.Single(reservation.Items);
        Assert.Equal(3, reservation.Items[0].Quantity);

        var movement = await db.InventoryMovements.SingleAsync(x =>
            x.ProductId == productId &&
            x.CorrelationId == orderId.ToString() &&
            x.MovementType == "Reserve");

        Assert.Equal(-3, movement.QuantityDelta);

        var outboxMessage = await db.OutboxMessages.SingleAsync(x =>
            x.EventType == "InventoryReserved" &&
            x.Payload.Contains(orderId.ToString()));

        Assert.Equal("inventory.events", outboxMessage.Topic);
    }

    [Fact]
    public async Task ReserveAsync_ShouldReturnIdempotentReplay_ForSameOrderId()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await SeedStockAsync(productId, availableQuantity: 10);

        Guid reservationId;

        await using (var scope = fixture.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IInventoryReservationStore>();

            var first = await store.ReserveAsync(
                orderId,
                [new ReserveStockItem(productId, 2)],
                CancellationToken.None);

            Assert.True(first.Success);
            reservationId = Guid.Parse(first.ReservationId);

            var second = await store.ReserveAsync(
                orderId,
                [new ReserveStockItem(productId, 2)],
                CancellationToken.None);

            Assert.True(second.Success);
            Assert.True(second.IsIdempotentReplay);
            Assert.Equal(reservationId.ToString(), second.ReservationId);
        }

        await using var assertScope = fixture.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var stock = await db.ProductStocks.SingleAsync(x => x.ProductId == productId);
        Assert.Equal(8, stock.AvailableQuantity);
        Assert.Equal(2, stock.ReservedQuantity);

        var outboxCount = await db.OutboxMessages.CountAsync(x =>
            x.EventType == "InventoryReserved" &&
            x.Payload.Contains(orderId.ToString()));

        Assert.Equal(1, outboxCount);
    }

    [Fact]
    public async Task ReserveAsync_ShouldFail_WhenProductStockNotFound()
    {
        var orderId = Guid.NewGuid();
        var missingProductId = Guid.NewGuid();

        ReserveInventoryResult result;

        await using (var scope = fixture.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IInventoryReservationStore>();
            result = await store.ReserveAsync(
                orderId,
                [new ReserveStockItem(missingProductId, 1)],
                CancellationToken.None);
        }

        Assert.False(result.Success);
        Assert.Equal(ReserveInventoryFailureReason.ProductNotFound, result.FailureReason);

        await using var assertScope = fixture.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var reservationExists = await db.InventoryReservations.AnyAsync(x => x.OrderId == orderId);
        Assert.False(reservationExists);
    }

    [Fact]
    public async Task ReserveAsync_ShouldFail_WhenStockIsInsufficient()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await SeedStockAsync(productId, availableQuantity: 1);

        ReserveInventoryResult result;

        await using (var scope = fixture.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IInventoryReservationStore>();
            result = await store.ReserveAsync(
                orderId,
                [new ReserveStockItem(productId, 2)],
                CancellationToken.None);
        }

        Assert.False(result.Success);
        Assert.Equal(ReserveInventoryFailureReason.InsufficientStock, result.FailureReason);

        await using var assertScope = fixture.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var stock = await db.ProductStocks.SingleAsync(x => x.ProductId == productId);
        Assert.Equal(1, stock.AvailableQuantity);
        Assert.Equal(0, stock.ReservedQuantity);
    }

    [Fact]
    public async Task ReleaseAsync_ShouldRestoreStock_AndMarkReservationReleased()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await SeedStockAsync(productId, availableQuantity: 10);

        Guid reservationId;

        await using (var scope = fixture.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IInventoryReservationStore>();
            var reserveResult = await store.ReserveAsync(
                orderId,
                [new ReserveStockItem(productId, 4)],
                CancellationToken.None);

            reservationId = Guid.Parse(reserveResult.ReservationId);

            var releaseResult = await store.ReleaseAsync(reservationId, CancellationToken.None);
            Assert.True(releaseResult.Success);
            Assert.False(releaseResult.IsIdempotentReplay);
        }

        await using var assertScope = fixture.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var stock = await db.ProductStocks.SingleAsync(x => x.ProductId == productId);
        Assert.Equal(10, stock.AvailableQuantity);
        Assert.Equal(0, stock.ReservedQuantity);

        var reservation = await db.InventoryReservations.SingleAsync(x => x.ReservationId == reservationId);
        Assert.Equal("Released", reservation.Status);

        var releaseMovement = await db.InventoryMovements.SingleAsync(x =>
            x.CorrelationId == reservationId.ToString() &&
            x.ProductId == productId &&
            x.MovementType == "Release");

        Assert.Equal(4, releaseMovement.QuantityDelta);

        var outboxMessage = await db.OutboxMessages.SingleAsync(x =>
            x.EventType == "InventoryReleased" &&
            x.Payload.Contains(reservationId.ToString()));

        Assert.Equal("inventory.events", outboxMessage.Topic);
    }

    [Fact]
    public async Task ReleaseAsync_ShouldReturnIdempotentSuccess_WhenReservationNotFound()
    {
        ReleaseInventoryResult result;

        await using (var scope = fixture.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IInventoryReservationStore>();
            result = await store.ReleaseAsync(Guid.NewGuid(), CancellationToken.None);
        }

        Assert.True(result.Success);
        Assert.True(result.IsIdempotentReplay);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReleaseAsync_ShouldReturnIdempotentSuccess_WhenReservationAlreadyReleased()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await SeedStockAsync(productId, availableQuantity: 5);

        Guid reservationId;

        await using (var scope = fixture.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IInventoryReservationStore>();
            var reserveResult = await store.ReserveAsync(
                orderId,
                [new ReserveStockItem(productId, 2)],
                CancellationToken.None);

            reservationId = Guid.Parse(reserveResult.ReservationId);

            var firstRelease = await store.ReleaseAsync(reservationId, CancellationToken.None);
            Assert.True(firstRelease.Success);

            var secondRelease = await store.ReleaseAsync(reservationId, CancellationToken.None);
            Assert.True(secondRelease.Success);
            Assert.True(secondRelease.IsIdempotentReplay);
        }

        await using var assertScope = fixture.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var releaseMovementCount = await db.InventoryMovements.CountAsync(x =>
            x.CorrelationId == reservationId.ToString() &&
            x.MovementType == "Release");

        Assert.Equal(1, releaseMovementCount);
    }

    private async Task SeedStockAsync(Guid productId, int availableQuantity, int reservedQuantity = 0)
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        db.ProductStocks.Add(new ProductStockEntity
        {
            ProductId = productId,
            AvailableQuantity = availableQuantity,
            ReservedQuantity = reservedQuantity,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
