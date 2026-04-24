using Application.DTOs;
using Application.Gateways.Exceptions;
using FluentAssertions;
using Grpc.Net.Client;
using Infrastructure.Gateways;
using Microsoft.Extensions.Logging.Abstractions;
using Protos.Inventory;
using Protos.Payment;
using Protos.Product;
using Xunit;

namespace Order.IntegrationTests.Gateways;

/*
 * both tests point at an unreachable gRPC endpoint and verify that the gateway's
 * error wrapping layer converts raw RpcExceptions into typed domain exception
*/

public sealed class GatewayTests
{
    // short connect timeout so tests fail fast rather than hanging
    private static GrpcChannel UnreachableChannel() =>
        GrpcChannel.ForAddress("http://localhost:19999",
            new GrpcChannelOptions
            {
                HttpHandler = new System.Net.Http.SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromMilliseconds(300)
                }
            });

    [Fact]
    public async Task PaymentGateway_ShouldThrowGatewayUnavailableException_OnGrpcUnavailable()
    {
        using var channel  = UnreachableChannel();
        var grpcClient = new PaymentService.PaymentServiceClient(channel);
        var gateway = new PaymentGateway(grpcClient, NullLogger<PaymentGateway>.Instance);

        Func<Task> act = () => gateway.ProcessPaymentAsync(
            orderId: Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            amount: 99.99m,
            currency: "USD",
            paymentMethod: "card",
            cancellationToken: CancellationToken.None);

        await act.Should()
            .ThrowAsync<GatewayUnavailableException>(
                "an unreachable payment service must yield GatewayUnavailableException, not a raw RpcException");
    }
    
    [Fact]
    public async Task InventoryGateway_ShouldThrowGatewayUnavailableException_OnGrpcUnavailable()
    {
        using var channel  = UnreachableChannel();
        var grpcClient = new InventoryService.InventoryServiceClient(channel);
        var gateway = new InventoryGateway(grpcClient, NullLogger<InventoryGateway>.Instance);

        Func<Task> act = () => gateway.ReserveAsync(
            orderId: Guid.NewGuid(),
            items: new List<OrderItemDto>
            {
                new(Guid.NewGuid(), Quantity: 1, Price: 10m, Currency: "USD")
            },
            cancellationToken: CancellationToken.None);

        await act.Should()
            .ThrowAsync<GatewayUnavailableException>(
                "an unreachable inventory service must yield GatewayUnavailableException, not a raw RpcException");
    }

    [Fact]
    public async Task ProductGateway_ShouldThrowGatewayUnavailableException_OnGrpcUnavailable()
    {
        using var channel = UnreachableChannel();
        var grpcClient = new ProductService.ProductServiceClient(channel);
        var gateway = new ProductGateway(grpcClient, NullLogger<ProductGateway>.Instance);

        Func<Task> act = () => gateway.GetCurrentPricesAsync(
            productIds: new[] { Guid.NewGuid() },
            cancellationToken: CancellationToken.None);

        await act.Should()
            .ThrowAsync<GatewayUnavailableException>(
                "an unreachable product service must yield GatewayUnavailableException, not a raw RpcException");
    }
}
