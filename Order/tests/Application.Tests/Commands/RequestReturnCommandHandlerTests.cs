using Application.Commands.RequestReturn;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Events.OrderReturn;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Commands;

public class RequestReturnCommandHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For <IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDbContextTransaction _transaction = Substitute.For<IDbContextTransaction>();
    private readonly ILogger<RequestReturnCommandHandler> _logger = Substitute.For<ILogger<RequestReturnCommandHandler>>();

    public RequestReturnCommandHandlerTests()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_transaction);
    }

    private Order CreateCompletedOrder()
    {
        var items = new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD")) };
        var order = Order.Create(CustomerId.CreateUnique(), Address.Create("S", "C", "C", "P"), items);
        order.Pay(PaymentId.From("PaymentId"));
        order.Approve();
        order.Complete();
        order.MarkEventsAsCommited();
        return order;
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenReturnIsRequestedSuccessfully()
    {
        var handler = new RequestReturnCommandHandler(_orderRepository, _outboxRepository, _unitOfWork, _logger);
        var order = CreateCompletedOrder();
        var command = CreateValidCommand(order.Id.Value, order.Items.First().ProductId.Value);

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        await _orderRepository.Received(1).AddAsync(
            Arg.Is<Order>(o => o.Status == OrderStatus.ReturnRequested),
            Arg.Any<CancellationToken>());

        await _outboxRepository.Received(1).AddAsync(
            Arg.Any<Guid>(),
            nameof(OrderReturnRequestedEvent),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOrderDoesNotExists()
    {
        var handler = new RequestReturnCommandHandler(_orderRepository, _outboxRepository, _unitOfWork, _logger);
        var command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid());
        
        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Order?)null);

        var result = await handler.Handle(command, CancellationToken.None);
        
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDomainLogicFails()
    {
        // create an order that is NOT completed (just pending)
        var handler = new RequestReturnCommandHandler(_orderRepository, _outboxRepository, _unitOfWork, _logger);
        var items = new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD")) };
        var order = Order.Create(CustomerId.CreateUnique(), Address.Create("S", "C", "C", "P"), items);

        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        var command = CreateValidCommand(order.Id.Value, items.First().ProductId.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRollbackAndReturnFailure_WhenOutboxFails()
    {
        var handler = new RequestReturnCommandHandler(_orderRepository, _outboxRepository, _unitOfWork, _logger);
        var order = CreateCompletedOrder();
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        _outboxRepository.AddAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>()).Throws(new Exception("Outbox DB connection lost"));

        var command = CreateValidCommand(order.Id.Value, order.Items.First().ProductId.Value);

        var result = await handler.Handle(command, CancellationToken.None);
        
        Assert.False(result.IsSuccess);
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());

    }

    private RequestReturnCommand CreateValidCommand(Guid orderId, Guid productId) =>
        new(
            orderId,
            "Item arrived broken",
            new List<OrderItemDto>
            {
                new(productId, 1, 100, "USD")
            });
}