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

public class CompleteReturnReceivedStepTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<CompleteReturnStep> _logger = Substitute.For<ILogger<CompleteReturnStep>>();
    private readonly IDbContextTransaction _transaction = Substitute.For<IDbContextTransaction>();

    private readonly CompleteReturnStep _step;

    public CompleteReturnReceivedStepTests()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_transaction);

        _step = new CompleteReturnStep(
            _orderRepository,
            _outboxRepository,
            _unitOfWork,
            _logger);
    }

    #region ExecuteAsync tests
    
    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenOrderIsCompleted()
    {
        var order = CreateRefundedOrder();
        var data = CreateSampleData(order.Id.Value);
        var context = new ReturnSagaContext { RefundId = "Ref123" };

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);


        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(order.Id.Value, result.Data?["OrderId"]);
        Assert.Equal("Returned", result.Data?["FinalStatus"]);
        Assert.Equal("Ref123", result.Data?["RefundId"]);
        Assert.Equal(data.RefundAmount, result.Data?["RefundAmount"]);
        Assert.Equal(OrderStatus.Returned, order.Status);

        await _outboxRepository.Received(1).AddAsync(
            Arg.Any<Guid>(),
            Arg.Is<string>(type => type.Contains("OrderReturnCompletedEvent")),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderNotFound()
    {
        var data = CreateSampleData(Guid.NewGuid());
        var context = new ReturnSagaContext();

        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Order?)null);

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);


        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRollback_WhenSaveChangesFails()
    {
        var order = CreateRefundedOrder();
        var data = CreateSampleData(order.Id.Value);
        var context = new ReturnSagaContext();
        var errorMessage = "Database connection lost";

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception(errorMessage));

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(errorMessage, result.ErrorMessage);

        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
    
    #endregion


    #region CompensateAsync tests

    // not ready right now 
    // [Fact]
    // public async Task CompensateAsync_ShouldRevertStatus_WhenOrderIsReturned()
    // {
    //     var order = CreateReturnedOrder();
    //     var data = CreateSampleData(order.Id.Value);
    //     var context = new ReturnSagaContext();
    //
    //     _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
    //         .Returns(order);
    //
    //     await _step.CompensateAsync(data, context, CancellationToken.None);
    //
    //     Assert.Equal(OrderStatus.Returned, order.Status);
    //
    //     await _outboxRepository.Received(1).AddAsync(
    //         Arg.Any<Guid>(),
    //         Arg.Any<string>(),
    //         Arg.Any<string>(),
    //         Arg.Any<DateTime>(),
    //         Arg.Any<CancellationToken>());
    //
    //     await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    //     await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    // }

    // not ready right now
    // [Fact]
    // public async Task CompensateAsync_ShouldDoNothing_WhenOrderNotFound()
    // {
    //     var data = CreateSampleData(Guid.NewGuid());
    //     var context = new ReturnSagaContext();
    //
    //     _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Order?)null);
    //
    //     await _step.CompensateAsync(data, context, CancellationToken.None);
    //     
    //     _logger.Received().Log(
    //         LogLevel.Warning,
    //         Arg.Any<EventId>(),
    //         Arg.Is<object>(o => o.ToString()!.Contains("not found")),
    //         null,
    //         Arg.Any<Func<object, Exception?, string>>()
    //     );
    //
    //     await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    //     await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    // }

    [Fact]
    public async Task CompensateAsync_ShouldDoNothing_WhenOrderNotInReturnedStatus()
    {
        var order = CreateRefundedOrder();
        var data = CreateSampleData(order.Id.Value);
        var context = new ReturnSagaContext();

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _step.CompensateAsync(data, context, CancellationToken.None);

        Assert.Equal(OrderStatus.Refunded, order.Status);

        Assert.Empty(order.UncommitedEvents);

        // await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
    
    
    private ReturnSagaData CreateSampleData(Guid correlationId)
    {
        return new ReturnSagaData
        {
            CorrelationId = correlationId,
            RefundAmount = 500m,
            Currency = "USD",
            ReturnReason = "Customer changed mind",
            CustomerId = Guid.NewGuid(),
            ReturnedItems = new List<OrderItemDto>
            {
                new(Guid.NewGuid(), 1, 500m, "USD")
            }
        };
    }

    private Order CreateRefundedOrder()
    {
        var order = Order.Create(
            CustomerId.CreateUnique(),
            Address.Create("123 Main St", "NYC", "USA", "10001"),
            new List<OrderItem>
            {
                OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(500, "USD"))
            });

        order.Pay(PaymentId.From("PAY-123"));
        order.Approve();
        order.Complete();
        order.RequestReturn("Defective", order.Items.ToList());
        order.ConfirmReturnReceived();
        order.ProcessRefund(PaymentId.From("REF-123"), Money.Create(500, "USD"));

        order.MarkEventsAsCommited();
        return order;
    }

    private Order CreateReturnedOrder()
    {
        var order = CreateRefundedOrder();
        order.CompleteReturn();
        order.MarkEventsAsCommited();
        return order;
    }
    
}