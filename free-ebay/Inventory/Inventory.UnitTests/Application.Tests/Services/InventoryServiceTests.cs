using Application.Interfaces;
using Application.Models;
using Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Application.Tests.Services;

public sealed class InventoryServiceTests
{
    private readonly IInventoryReservationStore store = Substitute.For<IInventoryReservationStore>();

    private InventoryService BuildService() =>
        new(store, NullLogger<InventoryService>.Instance);

    [Fact]
    public async Task ReserveAsync_ShouldFail_WhenOrderIdIsEmpty()
    {
        var command = new ReserveInventoryCommand(
            Guid.Empty,
            [new ReserveInventoryItemInput(Guid.NewGuid(), 1)]);

        var result = await BuildService().ReserveAsync(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ReserveInventoryFailureReason.Validation, result.FailureReason);
        Assert.Contains("OrderId", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        await store.DidNotReceive().ReserveAsync(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<ReserveStockItem>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveAsync_ShouldFail_WhenNoItemsProvided()
    {
        var command = new ReserveInventoryCommand(Guid.NewGuid(), []);

        var result = await BuildService().ReserveAsync(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ReserveInventoryFailureReason.Validation, result.FailureReason);
        Assert.Contains("At least one", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        await store.DidNotReceive().ReserveAsync(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<ReserveStockItem>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveAsync_ShouldNormalizeDuplicateProducts_BeforePassingToStore()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        store.ReserveAsync(
                orderId,
                Arg.Any<IReadOnlyCollection<ReserveStockItem>>(),
                Arg.Any<CancellationToken>())
            .Returns(ReserveInventoryResult.Successful(Guid.NewGuid().ToString()));

        var command = new ReserveInventoryCommand(
            orderId,
            [
                new ReserveInventoryItemInput(productId, 2),
                new ReserveInventoryItemInput(productId, 3)
            ]);

        var result = await BuildService().ReserveAsync(command, CancellationToken.None);

        Assert.True(result.Success);

        await store.Received(1).ReserveAsync(
            orderId,
            Arg.Is<IReadOnlyCollection<ReserveStockItem>>(items =>
                items.Count == 1 &&
                items.Single().ProductId == productId &&
                items.Single().Quantity == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveAsync_ShouldFail_WhenAnyProductIdIsEmpty()
    {
        var command = new ReserveInventoryCommand(
            Guid.NewGuid(),
            [new ReserveInventoryItemInput(Guid.Empty, 1)]);

        var result = await BuildService().ReserveAsync(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ReserveInventoryFailureReason.Validation, result.FailureReason);
        Assert.Contains("product_id", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        await store.DidNotReceive().ReserveAsync(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<ReserveStockItem>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveAsync_ShouldFail_WhenAnyQuantityIsNotPositive()
    {
        var command = new ReserveInventoryCommand(
            Guid.NewGuid(),
            [new ReserveInventoryItemInput(Guid.NewGuid(), 0)]);

        var result = await BuildService().ReserveAsync(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ReserveInventoryFailureReason.Validation, result.FailureReason);
        Assert.Contains("quantity", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        await store.DidNotReceive().ReserveAsync(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<ReserveStockItem>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseAsync_ShouldFail_WhenReservationIdIsEmpty()
    {
        var result = await BuildService().ReleaseAsync(Guid.Empty, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("ReservationId", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        await store.DidNotReceive().ReleaseAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmAsync_ShouldFail_WhenReservationIdIsEmpty()
    {
        var result = await BuildService().ConfirmAsync(Guid.Empty, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("ReservationId", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        await store.DidNotReceive().ConfirmAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmAsync_ShouldDelegateToStore_ForValidReservationId()
    {
        var reservationId = Guid.NewGuid();
        var expected = ReleaseInventoryResult.Successful();

        store.ConfirmAsync(reservationId, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await BuildService().ConfirmAsync(reservationId, CancellationToken.None);

        Assert.True(result.Success);
        await store.Received(1).ConfirmAsync(reservationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseAsync_ShouldDelegateToStore_ForValidReservationId()
    {
        var reservationId = Guid.NewGuid();
        var expected = ReleaseInventoryResult.Successful();

        store.ReleaseAsync(reservationId, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await BuildService().ReleaseAsync(reservationId, CancellationToken.None);

        Assert.True(result.Success);
        await store.Received(1).ReleaseAsync(reservationId, Arg.Any<CancellationToken>());
    }
}
