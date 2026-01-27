using Application.DTOs;
using Application.Interfaces;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.OrderSagaSteps;

public class UpdateOrderStatusStepTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDbContextTransaction _transaction = Substitute.For<IDbContextTransaction>();
    private readonly ILogger<UpdateOrderStatusStep> _logger = Substitute.For<ILogger<UpdateOrderStatusStep>>();
    private readonly UpdateOrderStatusStep _step;

    public UpdateOrderStatusStepTests()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_transaction);
        _step = new UpdateOrderStatusStep(_orderRepository, _outboxRepository, _unitOfWork, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenOrderIsUpdated()
    {
        var context = new OrderSagaContext { PaymentId = "PaymentId" };
        var order = CreateSampleOrder();
        var data = CreateSampleSaga(order.Id.Value);
        
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.True(result.Success);
        Assert.Equal("Paid", result.Data?["Status"]);
        
        Assert.Equal(OrderStatus.Paid, order.Status);
        await _orderRepository.Received(1).AddAsync(order, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync((Arg.Any<CancellationToken>()));

        await _outboxRepository.Received().AddAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());

    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderDoesNotExists()
    {
        var data = CreateSampleSaga();
        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((Order)null!);

        var result = await _step.ExecuteAsync(data, new OrderSagaContext(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenGenerateExceptionOccurs()
    {
        var data = CreateSampleSaga();
        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Database timeout"));

        var result = await _step.ExecuteAsync(data, new OrderSagaContext(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains("Database timeout", result.ErrorMessage);
    }

    [Fact]
    public async Task CompensateAsync_ShouldCancelOrder_whenOrderExists()
    {
        var data = CreateSampleSaga();
        var order = CreateSampleOrder();

        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);

         await _step.CompensateAsync(data, new OrderSagaContext(), CancellationToken.None);

        await _orderRepository.Received(1).AddAsync(order, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotThrow_WhenOrderNotFound()
    {
        var data = CreateSampleSaga();
        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Order)null!);

        var exception = await Record.ExceptionAsync(() =>
            _step.CompensateAsync(data, new OrderSagaContext(), CancellationToken.None));
        
        Assert.Null(exception);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldCatchAndLog_WhenRepositoryFails()
    {
        var data = CreateSampleSaga();
        var errorMessage = "errorMessage";
        
        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Throws(new Exception(errorMessage));

        var result = await _step.ExecuteAsync(data, new OrderSagaContext(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains(errorMessage, result.ErrorMessage);
        
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());

    }


    private static OrderSagaData CreateSampleSaga(Guid? correlationId = null)
    {
        return new OrderSagaData
        {
            CorrelationId = correlationId ?? Guid.NewGuid(),
            DeliveryAddress = new AddressDto("Street", "City", "Country", "12345"),
            Items = new List<OrderItemDto>()
        };
    }
    
    private static Order CreateSampleOrder()
    {
        var customerId = CustomerId.CreateUnique();
        var address = Address.Create("Zizkova 18", "Prague", "Czech Republic", "18000");

        var productId = ProductId.CreateUnique();
        var price = Money.Create(100, "USD");
        var items = new List<OrderItem>() { OrderItem.Create(productId, 2, price) };
        return Order.Create(customerId, address, items);
    }
}