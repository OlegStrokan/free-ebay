using Grpc.Core;
using Inventory.E2ETests.Infrastructure;
using Protos.Inventory;
using Xunit.Abstractions;

namespace Inventory.E2ETests.Tests;

[Collection("E2E")]
public sealed class InventoryGrpcE2ETests : IClassFixture<E2ETestServer>, IAsyncLifetime
{
    private readonly E2ETestServer server;
    private readonly ITestOutputHelper output;
    private InventoryService.InventoryServiceClient client = null!;

    public InventoryGrpcE2ETests(E2ETestServer server, ITestOutputHelper output)
    {
        this.server = server;
        this.output = output;
    }

    public async Task InitializeAsync()
    {
        await server.ResetAsync();
        client = server.CreateInventoryClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ReserveThenRelease_ShouldUpdateStockAndReservationState()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await server.SeedStockAsync(productId, availableQuantity: 5);

        var reserveResponse = await client.ReserveInventoryAsync(new ReserveInventoryRequest
        {
            OrderId = orderId.ToString(),
            Items =
            {
                new InventoryItem
                {
                    ProductId = productId.ToString(),
                    Quantity = 2
                }
            }
        });

        Assert.True(reserveResponse.Success);
        Assert.True(Guid.TryParse(reserveResponse.ReservationId, out var reservationId));

        var stockAfterReserve = await server.GetStockAsync(productId);
        Assert.NotNull(stockAfterReserve);
        Assert.Equal(3, stockAfterReserve!.AvailableQuantity);
        Assert.Equal(2, stockAfterReserve.ReservedQuantity);

        var reservationAfterReserve = await server.GetReservationAsync(reservationId);
        Assert.NotNull(reservationAfterReserve);
        Assert.Equal("Active", reservationAfterReserve!.Status);

        var releaseResponse = await client.ReleaseInventoryAsync(new ReleaseInventoryRequest
        {
            ReservationId = reservationId.ToString()
        });

        Assert.True(releaseResponse.Success);

        var stockAfterRelease = await server.GetStockAsync(productId);
        Assert.NotNull(stockAfterRelease);
        Assert.Equal(5, stockAfterRelease!.AvailableQuantity);
        Assert.Equal(0, stockAfterRelease.ReservedQuantity);

        var reservationAfterRelease = await server.GetReservationAsync(reservationId);
        Assert.NotNull(reservationAfterRelease);
        Assert.Equal("Released", reservationAfterRelease!.Status);

        output.WriteLine($"Reservation {reservationId} reserved and released successfully.");
    }

    [Fact]
    public async Task ReserveInventory_ShouldReturnInvalidArgument_WhenOrderIdIsInvalid()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            client.ReserveInventoryAsync(new ReserveInventoryRequest
            {
                OrderId = "bad-guid",
                Items =
                {
                    new InventoryItem
                    {
                        ProductId = Guid.NewGuid().ToString(),
                        Quantity = 1
                    }
                }
            }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task ReserveInventory_ShouldReturnFailedPrecondition_WhenStockIsInsufficient()
    {
        var productId = Guid.NewGuid();
        await server.SeedStockAsync(productId, availableQuantity: 1);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            client.ReserveInventoryAsync(new ReserveInventoryRequest
            {
                OrderId = Guid.NewGuid().ToString(),
                Items =
                {
                    new InventoryItem
                    {
                        ProductId = productId.ToString(),
                        Quantity = 3
                    }
                }
            }).ResponseAsync);

        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
    }

    [Fact]
    public async Task ReleaseInventory_ShouldReturnSuccess_WhenReservationDoesNotExist()
    {
        var response = await client.ReleaseInventoryAsync(new ReleaseInventoryRequest
        {
            ReservationId = Guid.NewGuid().ToString()
        });

        Assert.True(response.Success);
        Assert.Contains("not found", response.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
