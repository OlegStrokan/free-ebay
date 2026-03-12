using FluentAssertions;
using Product.E2ETests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Product.E2ETests.Tests;

[Collection("E2E")]
public class ProductOutboxE2ETests : IClassFixture<E2ETestServer>, IAsyncLifetime
{
    private readonly E2ETestServer _server;
    private readonly ITestOutputHelper _output;

    public ProductOutboxE2ETests(E2ETestServer server, ITestOutputHelper output)
    {
        _server = server;
        _output = output;
    }

    public async Task InitializeAsync() => await _server.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateProduct_ShouldPublishProductCreatedEvent_ToKafka()
    {
        _output.WriteLine("Outbox - ProductCreatedEvent published after product creation");

        var categoryId = await _server.SeedCategoryAsync("Electronics");
        var sellerId   = Guid.NewGuid();

        var productId = await _server.CreateProductAsync(
            sellerId, categoryId, name: "Outbox Test Product");

        var received = await _server.WaitForKafkaEventAsync(
            productId, "ProductCreatedEvent", timeoutSeconds: 20);

        received.Should().BeTrue(
            $"a ProductCreatedEvent keyed by productId {productId} must appear on Kafka");

        _output.WriteLine($"PASSED: ProductCreatedEvent received for {productId}");
    }

    [Fact]
    public async Task ActivateProduct_ShouldPublishProductStatusChangedEvent_ToKafka()
    {
        _output.WriteLine("Outbox - ProductStatusChangedEvent published after activation");

        var categoryId = await _server.SeedCategoryAsync();
        var sellerId   = Guid.NewGuid();

        var productId = await _server.CreateProductAsync(sellerId, categoryId);

        await _server.ActivateProductAsync(productId);

        var received = await _server.WaitForKafkaEventAsync(
            productId, "ProductStatusChangedEvent", timeoutSeconds: 20);

        received.Should().BeTrue(
            $"a ProductStatusChangedEvent for product {productId} must appear on Kafka");

        _output.WriteLine($"PASSED: ProductStatusChangedEvent received for {productId}");
    }

    [Fact]
    public async Task DeactivateProduct_ShouldPublishProductStatusChangedEvent_ToKafka()
    {
        _output.WriteLine("Outbox - ProductStatusChangedEvent published after deactivation");

        var categoryId = await _server.SeedCategoryAsync();
        var sellerId   = Guid.NewGuid();

        var productId = await _server.CreateProductAsync(sellerId, categoryId);
        await _server.ActivateProductAsync(productId);

        // Drain the activation event first so we wait for the deactivation one
        await _server.WaitForKafkaEventAsync(
            productId, "ProductStatusChangedEvent", timeoutSeconds: 20);

        await _server.DeactivateProductAsync(productId);

        var received = await _server.WaitForKafkaEventAsync(
            productId, "ProductStatusChangedEvent", timeoutSeconds: 20);

        received.Should().BeTrue(
            $"a ProductStatusChangedEvent for deactivated product {productId} must appear on Kafka");

        _output.WriteLine($"PASSED: deactivation event received for {productId}");
    }

    [Fact]
    public async Task UpdateStock_ShouldPublishStockUpdatedEvent_ToKafka()
    {
        _output.WriteLine("Outbox — StockUpdatedEvent published after stock change");

        var categoryId = await _server.SeedCategoryAsync();
        var sellerId   = Guid.NewGuid();

        var productId = await _server.CreateProductAsync(sellerId, categoryId);

        await _server.UpdateStockAsync(productId, newQuantity: 50);

        var received = await _server.WaitForKafkaEventAsync(
            productId, "StockUpdatedEvent", timeoutSeconds: 20);

        received.Should().BeTrue(
            $"a StockUpdatedEvent for product {productId} must appear on Kafka");

        _output.WriteLine($"PASSED: StockUpdatedEvent received for {productId}");
    }

    [Fact]
    public async Task MultipleEvents_FromSequentialCommands_AllArrivedOnKafka()
    {
        _output.WriteLine("Outbox — sequential commands produce multiple distinct events");

        var categoryId = await _server.SeedCategoryAsync();
        var sellerId   = Guid.NewGuid();

        var productId = await _server.CreateProductAsync(sellerId, categoryId);
        await _server.ActivateProductAsync(productId);
        await _server.UpdateStockAsync(productId, newQuantity: 10);

        var createdReceived = await _server.WaitForKafkaEventAsync(
            productId, "ProductCreatedEvent", timeoutSeconds: 20);
        var statusReceived = await _server.WaitForKafkaEventAsync(
            productId, "ProductStatusChangedEvent", timeoutSeconds: 20);
        var stockReceived = await _server.WaitForKafkaEventAsync(
            productId, "StockUpdatedEvent", timeoutSeconds: 20);

        createdReceived.Should().BeTrue("ProductCreatedEvent must be published");
        statusReceived.Should().BeTrue("ProductStatusChangedEvent must be published");
        stockReceived.Should().BeTrue("StockUpdatedEvent must be published");

        _output.WriteLine($"PASSED: all three events arrived for {productId}");
    }
}
