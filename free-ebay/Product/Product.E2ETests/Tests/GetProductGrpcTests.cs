using Api.Mappers;
using FluentAssertions;
using Grpc.Core;
using Product.E2ETests.Infrastructure;
using Protos.Product;
using Xunit;
using Xunit.Abstractions;

namespace Product.E2ETests.Tests;

[Collection("E2E")]
public class GetProductGrpcTests : IClassFixture<E2ETestServer>, IAsyncLifetime
{
    private readonly E2ETestServer _server;
    private readonly ITestOutputHelper _output;
    private ProductService.ProductServiceClient _client = null!;

    public GetProductGrpcTests(E2ETestServer server, ITestOutputHelper output)
    {
        _server = server;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        await _server.ResetAsync();
        _client = _server.CreateProductClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetProduct_ActiveProduct_ReturnsCorrectDetails()
    {
        _output.WriteLine("GetProduct - happy path");

        var categoryId = await _server.SeedCategoryAsync("Tools");
        var sellerId   = Guid.NewGuid();

        var productId = await _server.CreateProductAsync(
            sellerId, categoryId,
            name: "Power Drill",
            description: "Cordless 18V drill",
            price: 89.99m,
            currency: "USD",
            stock: 25);

        await _server.ActivateProductAsync(productId);

        var response = await _client.GetProductAsync(new GetProductRequest
        {
            ProductId = productId.ToString()
        });

        response.Product.Should().NotBeNull();
        response.Product.ProductId.Should().Be(productId.ToString());
        response.Product.Name.Should().Be("Power Drill");
        response.Product.Description.Should().Be("Cordless 18V drill");
        response.Product.CategoryId.Should().Be(categoryId.ToString());
        response.Product.CategoryName.Should().Be("Tools");
        response.Product.Price.ToDecimal().Should().BeApproximately(89.99m, 0.01m);
        response.Product.Currency.Should().Be("USD");
        response.Product.Stock.Should().Be(25);

        _output.WriteLine($"PASSED: GetProduct returned {response.Product.Name}");
    }

    [Fact]
    public async Task GetProduct_NonExistentProduct_ReturnsNotFoundStatus()
    {
        _output.WriteLine("GetProduct - not found");

        var act = async () => await _client.GetProductAsync(new GetProductRequest
        {
            ProductId = Guid.NewGuid().ToString()
        });

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.NotFound);

        _output.WriteLine("PASSED: NotFound returned as expected");
    }

    [Fact]
    public async Task GetProduct_InvalidGuidFormat_ReturnsInvalidArgumentStatus()
    {
        _output.WriteLine("GetProduct - invalid GUID");

        var act = async () => await _client.GetProductAsync(new GetProductRequest
        {
            ProductId = "not-a-valid-guid"
        });

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);

        _output.WriteLine("PASSED: InvalidArgument returned for bad GUID");
    }

    [Fact]
    public async Task GetProduct_DraftProduct_ReturnsDraftProduct()
    {
        _output.WriteLine("GetProduct - draft product (not yet activated)");

        var categoryId = await _server.SeedCategoryAsync("Electronics");
        var productId  = await _server.CreateProductAsync(
            Guid.NewGuid(), categoryId, name: "Draft Item", stock: 0);

        // Draft product is readable via GetProduct even before activation
        var response = await _client.GetProductAsync(new GetProductRequest
        {
            ProductId = productId.ToString()
        });

        response.Product.Name.Should().Be("Draft Item");
        response.Product.Stock.Should().Be(0);

        _output.WriteLine("PASSED: Draft product is readable");
    }
}
