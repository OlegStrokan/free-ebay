using Application.Gateways.Exceptions;
using Grpc.Core;
using Infrastructure.Gateways;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Protos.Product;

namespace Infrastructure.Tests.Gateways;

public class ProductGatewayTests
{
    private readonly ProductService.ProductServiceClient _client =
        Substitute.For<ProductService.ProductServiceClient>();

    private readonly ILogger<ProductGateway> _logger =
        Substitute.For<ILogger<ProductGateway>>();

    private ProductGateway Build() => new(_client, _logger);

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

    private static readonly Guid ProductId1 = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid ProductId2 = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    [Fact]
    public async Task GetCurrentPricesAsync_ShouldReturnPrices_WhenSucceeds()
    {
        var response = new GetProductPricesResponse();
        response.Prices.Add(new ProductPrice { ProductId = ProductId1.ToString(), Price = 29.99, Currency = "USD" });
        response.Prices.Add(new ProductPrice { ProductId = ProductId2.ToString(), Price = 9.50,  Currency = "USD" });

        _client
            .GetProductPricesAsync(Arg.Any<GetProductPricesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(response));

        var result = await Build().GetCurrentPricesAsync(
            new[] { ProductId1, ProductId2 }, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(29.99m, result.First(p => p.ProductId == ProductId1).Price);
        Assert.Equal(9.50m,  result.First(p => p.ProductId == ProductId2).Price);
        Assert.All(result, p => Assert.Equal("USD", p.Currency));
    }

    [Fact]
    public async Task GetCurrentPricesAsync_ShouldThrowProductNotFoundException_WhenResponseHasNotFoundIds()
    {
        var response = new GetProductPricesResponse();
        response.NotFoundIds.Add(ProductId1.ToString());

        _client
            .GetProductPricesAsync(Arg.Any<GetProductPricesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcCall(response));

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            Build().GetCurrentPricesAsync(new[] { ProductId1 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetCurrentPricesAsync_ShouldThrowProductNotFoundException_WhenRpcNotFound()
    {
        _client
            .GetProductPricesAsync(Arg.Any<GetProductPricesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<GetProductPricesResponse>(StatusCode.NotFound, "product not found"));

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            Build().GetCurrentPricesAsync(new[] { ProductId1 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetCurrentPricesAsync_ShouldThrowGatewayUnavailableException_WhenRpcUnavailable()
    {
        _client
            .GetProductPricesAsync(Arg.Any<GetProductPricesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<GetProductPricesResponse>(StatusCode.Unavailable, "product service down"));

        await Assert.ThrowsAsync<GatewayUnavailableException>(() =>
            Build().GetCurrentPricesAsync(new[] { ProductId1 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetCurrentPricesAsync_ShouldThrowGatewayUnavailableException_WhenRpcDeadlineExceeded()
    {
        _client
            .GetProductPricesAsync(Arg.Any<GetProductPricesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcFail<GetProductPricesResponse>(StatusCode.DeadlineExceeded, "timed out"));

        await Assert.ThrowsAsync<GatewayUnavailableException>(() =>
            Build().GetCurrentPricesAsync(new[] { ProductId1 }, CancellationToken.None));
    }
}
