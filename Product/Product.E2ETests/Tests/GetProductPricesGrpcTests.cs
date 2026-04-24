using Api.Mappers;
using FluentAssertions;
using Product.E2ETests.Infrastructure;
using Protos.Product;
using Xunit;
using Xunit.Abstractions;

namespace Product.E2ETests.Tests;

[Collection("E2E")]
public class GetProductPricesGrpcTests : IClassFixture<E2ETestServer>, IAsyncLifetime
{
    private readonly E2ETestServer _server;
    private readonly ITestOutputHelper _output;
    private ProductService.ProductServiceClient _client = null!;

    public GetProductPricesGrpcTests(E2ETestServer server, ITestOutputHelper output)
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
    public async Task GetProductPrices_KnownProducts_ReturnsPricesWithCorrectDecimalPrecision()
    {
        _output.WriteLine("GetProductPrices - happy path with decimal precision");

        var categoryId = await _server.SeedCategoryAsync("Electronics");
        var sellerId   = Guid.NewGuid();

        var id1 = await _server.CreateProductAsync(sellerId, categoryId, name: "Keyboard", price: 79.99m,  currency: "USD");
        var id2 = await _server.CreateProductAsync(sellerId, categoryId, name: "Monitor",  price: 349.50m, currency: "USD");

        var request = new GetProductPricesRequest();
        request.ProductIds.Add(id1.ToString());
        request.ProductIds.Add(id2.ToString());

        var response = await _client.GetProductPricesAsync(request);

        response.Prices.Should().HaveCount(2);

        var keyboardPrice = response.Prices.Single(p => p.ProductId == id1.ToString());
        keyboardPrice.Price.ToDecimal().Should().BeApproximately(79.99m, 0.01m);
        keyboardPrice.Currency.Should().Be("USD");

        var monitorPrice = response.Prices.Single(p => p.ProductId == id2.ToString());
        monitorPrice.Price.ToDecimal().Should().BeApproximately(349.50m, 0.01m);
        monitorPrice.Currency.Should().Be("USD");

        _output.WriteLine("PASSED: prices match with correct decimal precision");
    }

    [Fact]
    public async Task GetProductPrices_MultiCurrencyProducts_ReturnsCorrectCurrencyPerProduct()
    {
        _output.WriteLine("GetProductPrices - multi-currency");

        var categoryId = await _server.SeedCategoryAsync();
        var sellerId   = Guid.NewGuid();

        var usdId = await _server.CreateProductAsync(sellerId, categoryId, name: "US Product", price: 10.00m, currency: "USD");
        var eurId = await _server.CreateProductAsync(sellerId, categoryId, name: "EU Product", price: 9.50m,  currency: "EUR");

        var request = new GetProductPricesRequest();
        request.ProductIds.Add(usdId.ToString());
        request.ProductIds.Add(eurId.ToString());

        var response = await _client.GetProductPricesAsync(request);

        response.Prices.Should().HaveCount(2);
        response.Prices.Single(p => p.ProductId == usdId.ToString()).Currency.Should().Be("USD");
        response.Prices.Single(p => p.ProductId == eurId.ToString()).Currency.Should().Be("EUR");

        _output.WriteLine("PASSED: currencies are product-specific");
    }

    [Fact]
    public async Task GetProductPrices_NoneFound_ReturnsEmptyList()
    {
        _output.WriteLine("GetProductPrices - all missing");

        var request = new GetProductPricesRequest();
        request.ProductIds.Add(Guid.NewGuid().ToString());
        request.ProductIds.Add(Guid.NewGuid().ToString());

        var response = await _client.GetProductPricesAsync(request);

        response.Prices.Should().BeEmpty(
            "non-existent product IDs produce no price entries");

        _output.WriteLine("PASSED: empty list returned for unknown IDs");
    }

    [Fact]
    public async Task GetProductPrices_PriceUpdated_ReturnsLatestPrice()
    {
        _output.WriteLine("GetProductPrices - price reflects post-update value");

        var categoryId = await _server.SeedCategoryAsync();
        var sellerId   = Guid.NewGuid();

        var productId = await _server.CreateProductAsync(
            sellerId, categoryId, name: "Repriced Item", price: 99.00m);

        // Update to a new price via MediatR
        using var scope = _server.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        await mediator.Send(new Application.Commands.UpdateProduct.UpdateProductCommand(
            productId,
            "Repriced Item",
            "Updated description",
            categoryId,
            150.00m,
            "USD",
            [],
            []));

        var request = new GetProductPricesRequest();
        request.ProductIds.Add(productId.ToString());

        var response = await _client.GetProductPricesAsync(request);

        response.Prices.Should().HaveCount(1);
        response.Prices[0].Price.ToDecimal().Should().BeApproximately(150.00m, 0.01m,
            "GetProductPrices must reflect the latest persisted price");

        _output.WriteLine("PASSED: updated price returned");
    }
}
