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

public class ValidateReturnRequestStepTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<ValidateReturnRequestStep> _logger = Substitute.For<ILogger<ValidateReturnRequestStep>>();
    private readonly ValidateReturnRequestStep _step;
    private readonly IDbContextTransaction _transaction = Substitute.For<IDbContextTransaction>();


    public ValidateReturnRequestStepTests()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_transaction);
        _step = new ValidateReturnRequestStep(_orderRepository, _outboxRepository, _unitOfWork, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenOrderIsCompletedAndValid()
    {
        var order = CreateCompleteOrder();

        var productToReturn = order.Items.First();

        var data = CreateSampleData(order.Id.Value, productToReturn.ProductId);

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);
        
        Assert.True(result.Success);
        Assert.Equal(data.CorrelationId, result.Data?["OrderId"]);
        Assert.Equal(OrderStatus.ReturnRequested, order.Status);

        await _orderRepository.Received(1).AddAsync(order, Arg.Any<CancellationToken>());
        await _outboxRepository.Received(1).AddAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            // @todo: fix eventType checking Arg.Is<string>(n => n.Contains("OrderReturnRequestEvent")),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderNotFound()
    {
        var data = CreateSampleData(Guid.NewGuid(), ProductId.CreateUnique());

        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenItemsDoNotMatchOrder()
    {
        var order = CreateCompleteOrder();

        var foreignProductId = ProductId.CreateUnique();
        var data = CreateSampleData(order.Id.Value, foreignProductId);

        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains("not part of this order", result.ErrorMessage);

        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenGeneralExceptionOccurs()
    {
        var exceptionMessage = "Db Connection lost";
        var data = CreateSampleData(Guid.NewGuid(), ProductId.CreateUnique());

        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Throws(new Exception(exceptionMessage));

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(exceptionMessage, result.ErrorMessage);
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }
    
    // compensation tests

    [Fact]
    public async Task CompensateAsync_ShouldDoNothingAndLog()
    {
        var data = CreateSampleData(Guid.NewGuid(), ProductId.CreateUnique());

        await _step.CompensateAsync(data, new ReturnSagaContext(), CancellationToken.None);

        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _orderRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
        
        // optional test
        _logger.Received().Log(
            LogLevel.Information, 
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("No compensation needed")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
    
    
    // helpers
    
    private static ReturnSagaData CreateSampleData(Guid correlationId, ProductId productId)
    {
        var items = new List<OrderItemDto>
        {
            new OrderItemDto(productId.Value, 1, 100, "USD")
        };
        
        return new ReturnSagaData
        {
            CorrelationId = correlationId,
            Currency = "USD",
            CustomerId = Guid.NewGuid(),
            RefundAmount = 200,
            ReturnedItems = items,
            ReturnReason = "This product is dogshit"
            
        };
    }

    private static Order CreatePendingOrder()
    {
        var customerId = CustomerId.CreateUnique();
        var address = Address.Create("Voctarova 3 (ta take na palmovci", "Praha", "Czech Republic", "18000");

        var productId = ProductId.CreateUnique();
        var price = Money.Create(100, "USD");
        var items = new List<OrderItem>() { OrderItem.Create(productId, 2, price) };
        return Order.Create(customerId, address, items);
    }

    private static Order CreateCompleteOrder()
    {
        var order = CreatePendingOrder();
        
        order.Pay(PaymentId.From("PAY-123"));
        order.Approve();
        order.Complete();
        order.MarkEventsAsCommited();

        return order;
    }
}