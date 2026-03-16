using Api.GrpcServices;
using Application.Interfaces;
using Application.Models;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Protos.Inventory;

namespace Api.Tests.GrpcServices;

public sealed class InventoryGrpcServiceTests
{
    private readonly IInventoryService inventoryService = Substitute.For<IInventoryService>();
    private readonly ILogger<InventoryGrpcService> logger = Substitute.For<ILogger<InventoryGrpcService>>();
    private readonly ServerCallContext callContext = Substitute.For<ServerCallContext>();

    private InventoryGrpcService BuildService() =>
        new(inventoryService, logger);

    [Fact]
    public async Task ReserveInventory_ShouldThrowInvalidArgument_WhenOrderIdIsInvalid()
    {
        var request = new ReserveInventoryRequest
        {
            OrderId = "not-a-guid",
            Items =
            {
                new InventoryItem { ProductId = Guid.NewGuid().ToString(), Quantity = 1 }
            }
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().ReserveInventory(request, callContext));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        await inventoryService.DidNotReceive().ReserveAsync(
            Arg.Any<ReserveInventoryCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveInventory_ShouldThrowInvalidArgument_WhenAnyProductIdIsInvalid()
    {
        var request = new ReserveInventoryRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Items =
            {
                new InventoryItem { ProductId = "bad-guid", Quantity = 1 }
            }
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().ReserveInventory(request, callContext));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        await inventoryService.DidNotReceive().ReserveAsync(
            Arg.Any<ReserveInventoryCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveInventory_ShouldReturnSuccess_WhenApplicationReturnsSuccess()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var reservationId = Guid.NewGuid().ToString();

        inventoryService
            .ReserveAsync(Arg.Any<ReserveInventoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(ReserveInventoryResult.Successful(reservationId));

        var response = await BuildService().ReserveInventory(
            new ReserveInventoryRequest
            {
                OrderId = orderId.ToString(),
                Items =
                {
                    new InventoryItem { ProductId = productId.ToString(), Quantity = 2 }
                }
            },
            callContext);

        Assert.True(response.Success);
        Assert.Equal(reservationId, response.ReservationId);
        Assert.Equal(string.Empty, response.ErrorMessage);

        await inventoryService.Received(1).ReserveAsync(
            Arg.Is<ReserveInventoryCommand>(cmd =>
                cmd.OrderId == orderId &&
                cmd.Items.Count == 1 &&
                cmd.Items.Single().ProductId == productId &&
                cmd.Items.Single().Quantity == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveInventory_ShouldReturnIdempotentMessage_WhenReplayDetected()
    {
        var reservationId = Guid.NewGuid().ToString();

        inventoryService
            .ReserveAsync(Arg.Any<ReserveInventoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(ReserveInventoryResult.Successful(reservationId, isIdempotentReplay: true));

        var response = await BuildService().ReserveInventory(
            new ReserveInventoryRequest
            {
                OrderId = Guid.NewGuid().ToString(),
                Items =
                {
                    new InventoryItem { ProductId = Guid.NewGuid().ToString(), Quantity = 1 }
                }
            },
            callContext);

        Assert.True(response.Success);
        Assert.Equal("Idempotent replay.", response.ErrorMessage);
    }

    [Theory]
    [InlineData(ReserveInventoryFailureReason.Validation, StatusCode.InvalidArgument)]
    [InlineData(ReserveInventoryFailureReason.ProductNotFound, StatusCode.NotFound)]
    [InlineData(ReserveInventoryFailureReason.InsufficientStock, StatusCode.FailedPrecondition)]
    [InlineData(ReserveInventoryFailureReason.Unknown, StatusCode.Unavailable)]
    public async Task ReserveInventory_ShouldMapFailureReason_ToExpectedRpcStatus(
        ReserveInventoryFailureReason failureReason,
        StatusCode expectedStatusCode)
    {
        inventoryService
            .ReserveAsync(Arg.Any<ReserveInventoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(ReserveInventoryResult.Failed("reservation failed", failureReason));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().ReserveInventory(
                new ReserveInventoryRequest
                {
                    OrderId = Guid.NewGuid().ToString(),
                    Items =
                    {
                        new InventoryItem { ProductId = Guid.NewGuid().ToString(), Quantity = 1 }
                    }
                },
                callContext));

        Assert.Equal(expectedStatusCode, ex.StatusCode);
    }

    [Fact]
    public async Task ReserveInventory_ShouldThrowUnavailable_WhenApplicationThrowsUnexpectedException()
    {
        inventoryService
            .ReserveAsync(Arg.Any<ReserveInventoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReserveInventoryResult>(new InvalidOperationException("boom")));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().ReserveInventory(
                new ReserveInventoryRequest
                {
                    OrderId = Guid.NewGuid().ToString(),
                    Items =
                    {
                        new InventoryItem { ProductId = Guid.NewGuid().ToString(), Quantity = 1 }
                    }
                },
                callContext));

        Assert.Equal(StatusCode.Unavailable, ex.StatusCode);
    }

    [Fact]
    public async Task ReleaseInventory_ShouldThrowInvalidArgument_WhenReservationIdIsInvalid()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().ReleaseInventory(
                new ReleaseInventoryRequest { ReservationId = "bad-guid" },
                callContext));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        await inventoryService.DidNotReceive().ReleaseAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseInventory_ShouldReturnResponse_WhenApplicationReturnsResult()
    {
        var reservationId = Guid.NewGuid();

        inventoryService
            .ReleaseAsync(reservationId, Arg.Any<CancellationToken>())
            .Returns(ReleaseInventoryResult.Successful(message: string.Empty));

        var response = await BuildService().ReleaseInventory(
            new ReleaseInventoryRequest { ReservationId = reservationId.ToString() },
            callContext);

        Assert.True(response.Success);
        Assert.Equal(string.Empty, response.ErrorMessage);
    }

    [Fact]
    public async Task ReleaseInventory_ShouldThrowUnavailable_WhenApplicationThrowsUnexpectedException()
    {
        inventoryService
            .ReleaseAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReleaseInventoryResult>(new InvalidOperationException("boom")));

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            BuildService().ReleaseInventory(
                new ReleaseInventoryRequest { ReservationId = Guid.NewGuid().ToString() },
                callContext));

        Assert.Equal(StatusCode.Unavailable, ex.StatusCode);
    }
}
