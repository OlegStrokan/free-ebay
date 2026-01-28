using Application.DTOs;
using Application.Gateways;
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

public class ProcessRefundStepTests
{
    private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<ProcessRefundStep> _logger = Substitute.For<ILogger<ProcessRefundStep>>();
    private readonly IDbContextTransaction _transaction = Substitute.For<IDbContextTransaction>();

    private readonly ProcessRefundStep _step;

    public ProcessRefundStepTests()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_transaction);

        _step = new ProcessRefundStep(
            _paymentGateway,
            _orderRepository,
            _outboxRepository,
            _unitOfWork,
            _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenPaymentAndPersistenceSucceed()
    {
        var order = CreateReturnReceivedOrder();
        var data = CreateSampleData(order.Id.Value);
        var context = new ReturnSagaContext();
        var expectedRefundId = "REF-999";

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _paymentGateway.RefundAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>()).Returns(expectedRefundId);

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(expectedRefundId, context.RefundId);
        Assert.Equal(expectedRefundId, result.Data?["RefundId"]);
        Assert.Equal(OrderStatus.Refunded, order.Status);

        await _paymentGateway.Received(1).RefundAsync(
            Arg.Is<string>(s => s.Contains(data.CorrelationId.ToString())),
            data.RefundAmount,
            Arg.Is<string>(s => s.Contains(data.ReturnReason)),
            Arg.Any<CancellationToken>());

        await _outboxRepository.Received(1).AddAsync(
            Arg.Any<Guid>(),
           // @todo: should have eventType? Arg.Is<string>(s => s.Contains("OrderRefundedAmount")),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPaymentGatewayFails()
    {
        var data = CreateSampleData(Guid.NewGuid());
        var errorMessage = "Payment Provider Unavailable";

        _paymentGateway.RefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()).Throws(new Exception(errorMessage));

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(errorMessage, result.ErrorMessage);

        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _orderRepository.DidNotReceiveWithAnyArgs()
            .GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderNotFound()
    {
        var data = CreateSampleData(Guid.NewGuid());

        _paymentGateway.RefundAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("RefOk");

        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);

        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenDbUpdateThrowsException()
    {
        var errorMessage = "Deadlock detected";
        
        var order = CreateReturnReceivedOrder();
        var data = CreateSampleData(order.Id.Value);

        _paymentGateway.RefundAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("RefOk");

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Throws(new Exception(errorMessage));

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(errorMessage, result.ErrorMessage);

        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }
    
    // compensation tests

    [Fact]
    public async Task CompensateAsync_ShouldLogCriticalWarning_WhenRefundExists()
    {
        var data = CreateSampleData(Guid.NewGuid());
        var context = new ReturnSagaContext { RefundId = "RefToReverse" };

        await _step.CompensateAsync(data, context, CancellationToken.None);
        
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("MANUAL INTERVENTION REQUIRED")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        await _paymentGateway.DidNotReceiveWithAnyArgs().RefundAsync(
            Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task CompensateAsync_ShouldDoNothing_WhenRefundIdIsEmpty()
    {
        var data = CreateSampleData(Guid.NewGuid());
        var context = new ReturnSagaContext { RefundId = null };

        await _step.CompensateAsync(data, context, CancellationToken.None);
        
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("No refund to reverse")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
        
        // ensure no "critical" logs 
        _logger.DidNotReceive().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

    }
    
    
    private ReturnSagaData CreateSampleData(Guid correlationId)
    {
        return new ReturnSagaData
        {
            CorrelationId = correlationId,
            RefundAmount = 100m,
            Currency = "USD",
            ReturnReason = "Defective Product",
            CustomerId = Guid.NewGuid(),
            ReturnedItems = new List<OrderItemDto>()
        };
    }

    private Order CreateReturnReceivedOrder()
    {
        var order = Order.Create(CustomerId.CreateUnique(),
            Address.Create("Abrachamovna", "Atlantic City", "United States of China", "1989"),
            new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD")) });

        order.Pay(PaymentId.From("PAY-1"));
        order.Approve();
        order.Complete();
        order.RequestReturn("Bad", order.Items.ToList());
        order.ConfirmReturnReceived();
        
        order.MarkEventsAsCommited();
        return order;
    }

}