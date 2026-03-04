using Application.Commands.RequestReturn;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities.Order;
using Domain.Entities.RequestReturn;
using Domain.Services;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class RequestReturnCommandHandlerTest
{

    private readonly IOrderPersistenceService _orderPersistenceService =
        Substitute.For<IOrderPersistenceService>();

    private readonly IIdempotencyRepository _idempotencyRepository =
        Substitute.For<IIdempotencyRepository>();

    private readonly IReturnRequestPersistenceService _returnRequestPersistenceService =
        Substitute.For<IReturnRequestPersistenceService>();

    private readonly ReturnPolicyService _returnPolicyService = new();
    private readonly ILogger<RequestReturnCommandHandler> _logger =
        Substitute.For<ILogger<RequestReturnCommandHandler>>();

    private RequestReturnCommandHandler BuildHandler() =>
        new(_orderPersistenceService, _idempotencyRepository, _returnRequestPersistenceService, _returnPolicyService, _logger);

    // -------------------------------------------------------------------------

    private static Order CreateCompletedOrder()
    {
        var items = new List<OrderItem>
        {
            OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD"))
        };
        var order = Order.Create(
            CustomerId.CreateUnique(),
            Address.Create("Baker St", "London", "UK", "NW1"),
            items);
        order.Pay(PaymentId.From("pay_123"));
        order.Approve();
        order.Complete();
        order.ClearUncommittedEvents();
        return order;
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenReturnIsRequestedSuccessfully()
    {
        var order = CreateCompletedOrder();
        var returnRequestId = Guid.NewGuid();

        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _orderPersistenceService
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(order);

        _returnRequestPersistenceService
            .CreateReturnRequestAsync(
                Arg.Any<RequestReturn>(),
                Arg.Any<string?>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(returnRequestId);

        var result = await BuildHandler().Handle(
            CreateValidCommand(order.Id.Value, order.Items.First().ProductId.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        await _returnRequestPersistenceService.Received(1).CreateReturnRequestAsync(
            Arg.Any<RequestReturn>(),
            Arg.Is<string?>(k => k == "idempotency-key"),
            Arg.Is<Guid?>(id => id == order.Id.Value),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnExistingId_WhenDuplicateIdempotencyKey()
    {
        var existingId = Guid.NewGuid();
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyRecord("idempotency-key", existingId, DateTime.UtcNow));

        var result = await BuildHandler().Handle(
            CreateValidCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingId, result.Value);

        // persistence must NOT be called
        await _returnRequestPersistenceService.DidNotReceive().CreateReturnRequestAsync(
            Arg.Any<RequestReturn>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOrderNotFound()
    {
        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _orderPersistenceService
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var result = await BuildHandler().Handle(
            CreateValidCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOrderIsNotCompleted()
    {
        // Order is Pending - not eligible for return
        var items = new List<OrderItem>
        {
            OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD"))
        };
        var pendingOrder = Order.Create(
            CustomerId.CreateUnique(),
            Address.Create("S", "C", "C", "P"),
            items);

        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _orderPersistenceService
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(pendingOrder);

        var result = await BuildHandler().Handle(
            CreateValidCommand(pendingOrder.Id.Value, items.First().ProductId.Value),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("must be completed", result.Error);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPersistenceThrows()
    {
        var order = CreateCompletedOrder();

        _idempotencyRepository
            .GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdempotencyRecord?)null);

        _orderPersistenceService
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(order);

        _returnRequestPersistenceService
            .CreateReturnRequestAsync(
                Arg.Any<RequestReturn>(),
                Arg.Any<string?>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("Persistence failure"));

        var result = await BuildHandler().Handle(
            CreateValidCommand(order.Id.Value, order.Items.First().ProductId.Value),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Persistence failure", result.Error);
    }

    // -------------------------------------------------------------------------

    private static RequestReturnCommand CreateValidCommand(Guid orderId, Guid productId) =>
        new(
            orderId,
            "Item arrived broken",
            new List<OrderItemDto> { new(productId, 1, 100, "USD") },
            "idempotency-key");
}