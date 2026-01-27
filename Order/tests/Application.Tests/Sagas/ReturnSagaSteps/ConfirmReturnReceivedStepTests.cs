using Application.DTOs;
using Application.Interfaces;
using Application.Sagas.ReturnSaga;
using Application.Sagas.ReturnSaga.Steps;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.ReturnSagaSteps;

public class ConfirmReturnReceivedStepTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<ConfirmReturnReceivedStep> _logger = Substitute.For<ILogger<ConfirmReturnReceivedStep>>();
    private IDbContextTransaction _transaction = Substitute.For<IDbContextTransaction>();
    private readonly ConfirmReturnReceivedStep _step;


    public ConfirmReturnReceivedStepTests()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);

        _step = new ConfirmReturnReceivedStep(
            _orderRepository,
            _outboxRepository,
            _unitOfWork,
            _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenOrderIsReturnRequested()
    {
        var order = CreateReturnRequestedOrder();
        var data = CreateSampleData(order.Id.Value);
        var context = new ReturnSagaContext { ReturnShipmentId = "ReturnShipmentId" };

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.True(result.Success);
        Assert.Equal("ReturnReceived", result.Data?["Status"]);
        
        Assert.Equal(OrderStatus.ReturnReceived, order.Status);

        await _orderRepository.Received(1).AddAsync(order, Arg.Any<CancellationToken>());

        await _outboxRepository.Received(1).AddAsync(
            Arg.Any<Guid>(),
            Arg.Is<string>(s => s.Contains("OrderReturnReceivedEvent")),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>()
        );

        await _unitOfWork.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.Received().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderNotFound()
    {
        var data = CreateSampleData(Guid.NewGuid());
        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Order?)null);

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenStatusIsInvalid()
    {
        // order is completed, but return hasn't been requested yet

        var order = CreateCompletedOrder();
        var data = CreateSampleData(order.Id.Value);

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("unexpected status", result.ErrorMessage);

        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenGeneralExceptionOccurs()
    {
        var exceptionMessage = "Database Error";
        
        var order = CreateReturnRequestedOrder();
        var data = CreateSampleData(order.Id.Value);

        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Throws(new Exception(exceptionMessage));

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(exceptionMessage, result.ErrorMessage);

        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleIdempotency_WhenStatusIsAlreadyReceived()
    {
        var order = CreateReturnReceivedOrder();
        var data = CreateSampleData(order.Id.Value);

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("AlreadyProcessed", result.Data?["Status"]);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }
    
    // compensation tests

    [Fact]
    public async Task CompensateAsync_ShouldRevertStatus_WhenOrderIsReturnReceived()
    {
        var order = CreateReturnReceivedOrder();
        var data = CreateSampleData(order.Id.Value);

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _step.CompensateAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.Equal(OrderStatus.ReturnRequested, order.Status);

        await _orderRepository.Received(1).AddAsync(order, Arg.Any<CancellationToken>());

        await _outboxRepository.Received(1).AddAsync(
            Arg.Any<Guid>(),
            Arg.Is<string>(s => s.Contains("OrderReturnReceiptRevertedEvent")),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldDoNothing_WhenStatusIsNotReturnReceived()
    {
        // if order is still in "returnRequested" (step failed before updating), we shouldn't rever anything

        var order = CreateReturnRequestedOrder();
        var data = CreateSampleData(order.Id.Value);

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _step.CompensateAsync(data, new ReturnSagaContext(), CancellationToken.None);

        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());

    }

    [Fact]
    public async Task CompensateAsync_ShouldRollback_WhenException()
    {
        var data = CreateSampleData(Guid.NewGuid());

        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Critical Fail"));

        await _step.CompensateAsync(data, new ReturnSagaContext(), CancellationToken.None);
        
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to compensate")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }
    
    
    
    private ReturnSagaData CreateSampleData(Guid correlationId)
    {
        return new ReturnSagaData
        {
            CorrelationId = correlationId,
            CustomerId = Guid.NewGuid(),
            ReturnReason = "Don't like it",
            RefundAmount = 100,
            Currency = "USD",
            ReturnedItems = new List<OrderItemDto>()
        };
    }
    
    // Helper to get Order to "Completed"
    private Order CreateCompletedOrder()
    {
        var customerId = CustomerId.CreateUnique();
        var address = Address.Create("Street", "City", "CZ", "11000");
        var items = new List<OrderItem> 
        { 
            OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD")) 
        };

        var order = Order.Create(customerId, address, items);
        order.Pay(PaymentId.From("PAY-1"));
        order.Approve();
        order.Complete();
        order.MarkEventsAsCommited();
        
        return order;
    }

    // Helper to get Order to "ReturnRequested"
    private Order CreateReturnRequestedOrder()
    {
        var order = CreateCompletedOrder();
        // Request return for all items
        order.RequestReturn("Wrong size", order.Items.ToList());
        order.MarkEventsAsCommited();
        return order;
    }

    // Helper to get Order to "ReturnReceived"
    private Order CreateReturnReceivedOrder()
    {
        var order = CreateReturnRequestedOrder();
        order.ConfirmReturnReceived();
        order.MarkEventsAsCommited();
        return order;
    }
    
}