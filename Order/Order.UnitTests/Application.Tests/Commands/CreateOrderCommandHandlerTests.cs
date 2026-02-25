using Application.Commands.CreateOrder;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Entities.Order;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly IOrderPersistenceService _orderPersistenceService =
        Substitute.For<IOrderPersistenceService>();

    private readonly IIdempotencyRepository _idempotencyRepository =
        Substitute.For<IIdempotencyRepository>();

    private readonly ILogger<CreateOrderCommandHandler> _logger =
        Substitute.For<ILogger<CreateOrderCommandHandler>>();

    private CreateOrderCommandHandler BuildHandler() =>
        new(_orderPersistenceService, _idempotencyRepository, _logger);

    [Fact]
    public async Task Handle_ShouldReturnSuccess_AndCallPersistence_WhenOrderIsCreated()
    {
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

        // persistence must NOT be called — idempotency short-circuits
        await _orderPersistenceService.DidNotReceive().CreateOrderAsync(
            Arg.Any<Order>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPersistenceThrows()
    {
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

    // -------------------------------------------------------------------------

    private static CreateOrderCommand CreateValidCommand() =>
        new(
            Guid.NewGuid(),
            new List<OrderItemDto> { new(Guid.NewGuid(), 2, 200, "USD") },
            new AddressDto("Baker St", "London", "UK", "NW1"),
            "Cart",
            "idempotency-key");
}