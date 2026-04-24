using Application.DTOs;
using Application.Gateways.Exceptions;
using Grpc.Core;
using Infrastructure.Gateways;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Protos.Inventory;

namespace Infrastructure.Tests.Gateways;

public class InventoryGatewayTests
{
    private readonly InventoryService.InventoryServiceClient _client =
        Substitute.For<InventoryService.InventoryServiceClient>();

    private readonly ILogger<InventoryGateway> _logger =
        Substitute.For<ILogger<InventoryGateway>>();

    private InventoryGateway Build() => new(_client, _logger);

    private static AsyncUnaryCall<T> GrpcCall<T>(T response) =>
        new AsyncUnaryCall<T>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

    private static AsyncUnaryCall<T> GrpcFail<T>(StatusCode code, string detail) =>
        new AsyncUnaryCall<T>(
            Task.FromException<T>(new RpcException(new Status(code, detail))),
            Task.FromResult(new Metadata()),
            () => new Status(code, detail),
            () => new Metadata(),
            () => { });

    private static List<OrderItemDto> SomeItems() =>
        new() { new OrderItemDto(Guid.NewGuid(), 2, 10m, "USD") };

    [Fact]
    public async Task ReserveAsync_ShouldReturnReservationId_WhenSucceeds()
    {
        var reservationId = "res-abc";
        _client
            .ReserveInventoryAsync(Arg.Any<ReserveInventoryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ReserveInventoryResponse { Success = true, ReservationId = reservationId }));

        var result = await Build().ReserveAsync(Guid.NewGuid(), SomeItems(), CancellationToken.None);

        Assert.Equal(reservationId, result);
    }

    [Fact]
    public async Task ReserveAsync_ShouldThrowInsufficientInventory_WhenNotSuccess()
    {
        _client
            .ReserveInventoryAsync(Arg.Any<ReserveInventoryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ReserveInventoryResponse { Success = false, ErrorMessage = "out of stock" }));

        await Assert.ThrowsAsync<InsufficientInventoryException>(() =>
            Build().ReserveAsync(Guid.NewGuid(), SomeItems(), CancellationToken.None));
    }

    [Fact]
    public async Task ReserveAsync_ShouldThrowInsufficientInventory_WhenRpcFailedPrecondition()
    {
        _client
            .ReserveInventoryAsync(Arg.Any<ReserveInventoryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<ReserveInventoryResponse>(StatusCode.FailedPrecondition, "insufficient stock"));

        await Assert.ThrowsAsync<InsufficientInventoryException>(() =>
            Build().ReserveAsync(Guid.NewGuid(), SomeItems(), CancellationToken.None));
    }

    [Fact]
    public async Task ReserveAsync_ShouldThrowInsufficientInventory_WhenRpcResourceExhausted()
    {
        _client
            .ReserveInventoryAsync(Arg.Any<ReserveInventoryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<ReserveInventoryResponse>(StatusCode.ResourceExhausted, "quota exceeded"));

        await Assert.ThrowsAsync<InsufficientInventoryException>(() =>
            Build().ReserveAsync(Guid.NewGuid(), SomeItems(), CancellationToken.None));
    }

    [Fact]
    public async Task ReserveAsync_ShouldThrowInsufficientInventory_WhenRpcNotFound()
    {
        _client
            .ReserveInventoryAsync(Arg.Any<ReserveInventoryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<ReserveInventoryResponse>(StatusCode.NotFound, "product not found"));

        await Assert.ThrowsAsync<InsufficientInventoryException>(() =>
            Build().ReserveAsync(Guid.NewGuid(), SomeItems(), CancellationToken.None));
    }

    [Fact]
    public async Task ReleaseReservationAsync_ShouldComplete_WhenSucceeds()
    {
        _client
            .ReleaseInventoryAsync(Arg.Any<ReleaseInventoryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ReleaseInventoryResponse { Success = true }));

        // Should not throw
        await Build().ReleaseReservationAsync("res-123", CancellationToken.None);
    }

    [Fact]
    public async Task ReleaseReservationAsync_ShouldThrowInvalidOperation_WhenNotSuccess()
    {
        _client
            .ReleaseInventoryAsync(Arg.Any<ReleaseInventoryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ReleaseInventoryResponse { Success = false, ErrorMessage = "already released" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Build().ReleaseReservationAsync("res-123", CancellationToken.None));
    }

    [Fact]
    public async Task ReleaseReservationAsync_ShouldNotThrow_WhenRpcNotFound_IdempotentRelease()
    {
        // NotFound = reservation already gone - treat as idempotent success (just log warning)
        _client
            .ReleaseInventoryAsync(Arg.Any<ReleaseInventoryRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<ReleaseInventoryResponse>(StatusCode.NotFound, "not found"));

        var exception = await Record.ExceptionAsync(() =>
            Build().ReleaseReservationAsync("res-gone", CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ConfirmReservationAsync_ShouldComplete_WhenSucceeds()
    {
        _client
            .ConfirmReservationAsync(Arg.Any<ConfirmReservationRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ConfirmReservationResponse { Success = true }));

        await Build().ConfirmReservationAsync("res-123", CancellationToken.None);
    }

    [Fact]
    public async Task ConfirmReservationAsync_ShouldThrowInvalidOperation_WhenNotSuccess()
    {
        _client
            .ConfirmReservationAsync(Arg.Any<ConfirmReservationRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(new ConfirmReservationResponse { Success = false, ErrorMessage = "expired" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Build().ConfirmReservationAsync("res-123", CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmReservationAsync_ShouldThrowInvalidOperation_WhenRpcFailedPrecondition()
    {
        _client
            .ConfirmReservationAsync(Arg.Any<ConfirmReservationRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<ConfirmReservationResponse>(StatusCode.FailedPrecondition, "expired"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Build().ConfirmReservationAsync("res-123", CancellationToken.None));
    }
}
