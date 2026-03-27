using Application.Commands.CreateOrder;
using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Entities.Order;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class CreateOrderCommandHandlerTests
{
    private static readonly Guid ProductId = Guid.Parse("11111111-0000-0000-0000-000000000001");

    private readonly IOrderPersistenceService _orderPersistenceService =
        Substitute.For<IOrderPersistenceService>();

    private readonly IIdempotencyRepository _idempotencyRepository =
        Substitute.For<IIdempotencyRepository>();

    private readonly IWriteRegionOwnershipResolver _writeRegionOwnershipResolver =
        Substitute.For<IWriteRegionOwnershipResolver>();

    private readonly IProductGateway _productGateway =
        Substitute.For<IProductGateway>();

    private readonly IUserGateway _userGateway =
        Substitute.For<IUserGateway>();

    private readonly ILogger<CreateOrderCommandHandler> _logger =
        Substitute.For<ILogger<CreateOrderCommandHandler>>();

    public CreateOrderCommandHandlerTests()
    {
        _writeRegionOwnershipResolver
            .ResolveForCustomer(Arg.Any<Guid>())
            .Returns(new WriteRegionOwnershipDecision(
                IsEnabled: false,
                IsCurrentRegionOwner: true,
                CurrentRegion: "local",
                OwnerRegion: "local"));
    }

    private CreateOrderCommandHandler BuildHandler() =>
        new(
            _orderPersistenceService,
            _idempotencyRepository,
            _writeRegionOwnershipResolver,
            _productGateway,
            _userGateway,
            _logger);

    // default setup: gateway returns 99.50 USD - intentionally different from the
    // 200 USD baked into CreateValidCommand() so tests can verify the override
    private void SetupProductGateway(decimal price = 99.50m, string currency = "USD")
    {
        _productGateway
            .GetCurrentPricesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductPriceDto> { new(ProductId, price, currency) });
    }

    private void SetupUserGateway(bool isActive = true)
    {
        _userGateway
            .GetUserProfileAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var customerId = callInfo.ArgAt<Guid>(0);
                return Task.FromResult(new UserProfileDto(
                    customerId,
                    $"{customerId}@test.local",
                    "Test Customer",
                    "US",
                    "Standard",
                    isActive));
            });
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_AndCallPersistence_WhenOrderIsCreated()
    {
        SetupProductGateway();
        SetupUserGateway();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        var result = await BuildHandler().Handle(CreateValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        await _orderPersistenceService.Received(1).CreateOrderAsync(
            Arg.Any<Order>(),
            Arg.Is<string>(k => k == "idempotency-key"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnExistingOrderId_WhenDuplicateIdempotencyKey()
    {
        var existingOrderId = Guid.NewGuid();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyRecord("idempotency-key", existingOrderId, DateTime.UtcNow));

        var result = await BuildHandler().Handle(CreateValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingOrderId, result.Value);

        await _orderPersistenceService.DidNotReceive().CreateOrderAsync(
            Arg.Any<Order>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await _userGateway.DidNotReceive().GetUserProfileAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());

        await _productGateway.DidNotReceive().GetCurrentPricesAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPersistenceThrows()
    {
        SetupProductGateway();
        SetupUserGateway();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _orderPersistenceService
            .CreateOrderAsync(Arg.Any<Order>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("DB is on fire"));

        var result = await BuildHandler().Handle(CreateValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("DB is on fire", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldUseGatewayPrice_NotClientSuppliedPrice()
    {
        // command sends 200 USD; gateway returns 99.50 USD
        // the order aggregate must be built with 99.50, proving price manipulation is blocked
        const decimal clientPrice  = 200m;
        const decimal gatewayPrice = 99.50m;

        SetupProductGateway(price: gatewayPrice);
        SetupUserGateway();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        Order? savedOrder = null;
        await _orderPersistenceService
            .CreateOrderAsync(
                Arg.Do<Order>(o => savedOrder = o),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new List<OrderItemDto> { new(ProductId, 2, clientPrice, "USD") },
            new AddressDto("Baker St", "London", "UK", "NW1"),
            "Cart",
            "idempotency-key");

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedOrder);
        // 2 units × 99.50 = 199.00 (NOT 2 × 200 = 400)
        Assert.Equal(199.00m, savedOrder!.TotalPrice.Amount);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductGatewayThrowsProductNotFoundException()
    {
        SetupUserGateway();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _productGateway
            .GetCurrentPricesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Throws(new ProductNotFoundException(["11111111-0000-0000-0000-000000000001"]));

        var result = await BuildHandler().Handle(CreateValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);

        await _orderPersistenceService.DidNotReceive().CreateOrderAsync(
            Arg.Any<Order>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductGatewayThrowsGatewayUnavailable()
    {
        SetupUserGateway();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _productGateway
            .GetCurrentPricesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Throws(new GatewayUnavailableException("Product Service is down"));

        var result = await BuildHandler().Handle(CreateValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Product Service is down", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCustomerIsBlocked()
    {
        SetupUserGateway(isActive: false);
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        var result = await BuildHandler().Handle(CreateValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("blocked", result.Error, StringComparison.OrdinalIgnoreCase);

        await _productGateway.DidNotReceive().GetCurrentPricesAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Any<CancellationToken>());

        await _orderPersistenceService.DidNotReceive().CreateOrderAsync(
            Arg.Any<Order>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRejectWrite_WhenCurrentRegionIsNotOwner()
    {
        _writeRegionOwnershipResolver
            .ResolveForCustomer(Arg.Any<Guid>())
            .Returns(new WriteRegionOwnershipDecision(
                IsEnabled: true,
                IsCurrentRegionOwner: false,
                CurrentRegion: "eu-west-1",
                OwnerRegion: "us-east-1"));

        var result = await BuildHandler().Handle(CreateValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Write ownership mismatch", result.Error, StringComparison.OrdinalIgnoreCase);

        await _idempotencyRepository.DidNotReceive().GetByKeyAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await _orderPersistenceService.DidNotReceive().CreateOrderAsync(
            Arg.Any<Order>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------

    private static CreateOrderCommand CreateValidCommand() =>
        new(
            Guid.NewGuid(),
            new List<OrderItemDto> { new(ProductId, 2, 200, "USD") },
            new AddressDto("Baker St", "London", "UK", "NW1"),
            "Cart",
            "idempotency-key");
}