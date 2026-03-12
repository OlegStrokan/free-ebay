using Api.Mappers;
using FluentAssertions;
using Grpc.Core;
using Product.E2ETests.Infrastructure;
using Protos.Product;
using Xunit;
using Xunit.Abstractions;

namespace Product.E2ETests.Tests;

[Collection("E2E")]
public class GetProductsGrpcTests : IClassFixture<E2ETestServer>, IAsyncLifetime
{
    private readonly E2ETestServer _server;
    private readonly ITestOutputHelper _output;
    private ProductService.ProductServiceClient _client = null!;

    public GetProductsGrpcTests(E2ETestServer server, ITestOutputHelper output)
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
    public async Task GetProducts_AllExistingIds_ReturnsAllDetails()
    {
        _output.WriteLine("GetProducts - all found");

        var categoryId = await _server.SeedCategoryAsync("Books");
        var sellerId   = Guid.NewGuid();

        var id1 = await _server.CreateProductAsync(sellerId, categoryId, name: "Clean Code",    price: 29.99m, stock: 50);
        var id2 = await _server.CreateProductAsync(sellerId, categoryId, name: "Pragmatic Programmer", price: 34.99m, stock: 30);

        var request = new GetProductsRequest();
        request.ProductIds.Add(id1.ToString());
        request.ProductIds.Add(id2.ToString());

        var response = await _client.GetProductsAsync(request);

        response.Products.Should().HaveCount(2);
        response.NotFoundIds.Should().BeEmpty("all requested IDs exist");

        var names = response.Products.Select(p => p.Name).ToList();
        names.Should().Contain("Clean Code");
        names.Should().Contain("Pragmatic Programmer");

        _output.WriteLine($"PASSED: returned {response.Products.Count} products");
    }

    [Fact]
    public async Task GetProducts_PartialMiss_ReturnsFoundProductsAndNotFoundIds()
    {
        _output.WriteLine("GetProducts - partial miss");

        var categoryId = await _server.SeedCategoryAsync("Software");
        var existingId = await _server.CreateProductAsync(
            Guid.NewGuid(), categoryId, name: "Existing Product");

        var missingId = Guid.NewGuid();

        var request = new GetProductsRequest();
        request.ProductIds.Add(existingId.ToString());
        request.ProductIds.Add(missingId.ToString());

        var response = await _client.GetProductsAsync(request);

        response.Products.Should().HaveCount(1);
        response.Products[0].ProductId.Should().Be(existingId.ToString());

        response.NotFoundIds.Should().ContainSingle()
            .Which.Should().Be(missingId.ToString());

        _output.WriteLine("PASSED: found=1, notFound=1");
    }

    [Fact]
    public async Task GetProducts_AllMissingIds_ReturnsEmptyProductsAndAllNotFound()
    {
        _output.WriteLine("GetProducts - all missing");

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var request = new GetProductsRequest();
        request.ProductIds.Add(id1.ToString());
        request.ProductIds.Add(id2.ToString());

        var response = await _client.GetProductsAsync(request);

        response.Products.Should().BeEmpty();
        response.NotFoundIds.Should().HaveCount(2);

        _output.WriteLine("PASSED: all IDs returned in NotFoundIds");
    }

    [Fact]
    public async Task GetProducts_WithAttributes_ReturnsAttributesCorrectly()
    {
        _output.WriteLine("GetProducts - attributes roundtrip");

        var productId = await _server.CreateProductAsync(
            Guid.NewGuid(), await _server.SeedCategoryAsync(),
            name: "Attributed Product",
            attributes: [new("color", "blue"), new("size", "M")]);

        var request = new GetProductsRequest();
        request.ProductIds.Add(productId.ToString());

        var response = await _client.GetProductsAsync(request);

        response.Products.Should().HaveCount(1);
        var attrs = response.Products[0].Attributes;
        attrs.Should().HaveCount(2);
        attrs.Should().Contain(a => a.Key == "color" && a.Value == "blue");
        attrs.Should().Contain(a => a.Key == "size" && a.Value == "M");

        _output.WriteLine("PASSED: attributes roundtrip correct");
    }
}
