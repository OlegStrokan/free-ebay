using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Interfaces;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.OrderSagaSteps;

public class CreateShipmentStepTests
{
    private readonly IShippingGateway _shippingGateway = Substitute.For<IShippingGateway>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<CreateShipmentStep> _logger = Substitute.For<ILogger<CreateShipmentStep>>();
    private readonly IDbContextTransaction _transaction = Substitute.For<IDbContextTransaction>();
    private readonly CreateShipmentStep _step;
    

    public CreateShipmentStepTests()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_transaction);

        _step = new CreateShipmentStep(
            _shippingGateway,
            _orderRepository,
            _outboxRepository,
            _unitOfWork,
            _logger
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenAllOperationsSucceed()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();
        var expectedShipmentId = "SHIP-123";
        var expectedTrackingNumber = Guid.NewGuid().ToString();

        var order = CreatePaidOrder(data.CorrelationId);
        _orderRepository.GetByIdAsync(OrderId.From(data.CorrelationId), Arg.Any<CancellationToken>())
            .Returns(order);

        _shippingGateway.CreateShipmentAsync(data.CorrelationId, data.DeliveryAddress, data.Items,
                Arg.Any<CancellationToken>())
            .Returns(expectedShipmentId);
        _shippingGateway.GetTrackingNumberAsync(expectedShipmentId, Arg.Any<CancellationToken>())
            .Returns(expectedTrackingNumber);

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.True(result.Success);
        Assert.Equal(expectedShipmentId, context.ShipmentId);

        await _shippingGateway.Received(1).CreateShipmentAsync(data.CorrelationId,
            data.DeliveryAddress, data.Items, Arg.Any<CancellationToken>());
        await _shippingGateway.Received(1).GetTrackingNumberAsync(expectedShipmentId, Arg.Any<CancellationToken>());

        await _orderRepository.Received(1).AddAsync(
            Arg.Is<Order>(o => o.TrackingId!.Value == expectedTrackingNumber), 
            Arg.Any<CancellationToken>());

        await _outboxRepository.Received(1).AddAsync(
            Arg.Any<Guid>(),
            Arg.Is<string>(s => s.Contains("OrderTrackingAssignedEvent")),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderNotFound()
    {
        var data = CreateSampleData();

        _shippingGateway.CreateShipmentAsync(default, default!, default!, default)
            .ReturnsForAnyArgs("SHIP-1");
        _shippingGateway.GetTrackingNumberAsync(default!, default)
            .ReturnsForAnyArgs(Guid.NewGuid().ToString());

        _orderRepository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var result = await _step.ExecuteAsync(data, new OrderSagaContext(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);

        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenAddressIsInvalid()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();

        _shippingGateway.CreateShipmentAsync(Arg.Any<Guid>(), Arg.Any<AddressDto>(),
                Arg.Any<List<OrderItemDto>>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidAddressException("Bad Address"));

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains("Bad Address", result.ErrorMessage);
        Assert.Null(context.ShipmentId);

        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenGetTrackingFails()
    {
        var data = CreateSampleData();
        var shipmentId = "SHIP-OK";

        _shippingGateway.CreateShipmentAsync(default, default!, default!, default)
            .ReturnsForAnyArgs(shipmentId);

        _shippingGateway.GetTrackingNumberAsync(shipmentId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Tracking API Down"));

        var result = await _step.ExecuteAsync(data, new OrderSagaContext(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains("Tracking API Down", result.ErrorMessage);
    }
    
    // compensation tests

    [Fact]
    public async Task CompensateAsync_ShouldCancelShipmentAndRevertTracking_WhenShipmentExists()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext { ShipmentId = "ShipmentToCancelId" };

        var order = CreatePaidOrder(data.CorrelationId);
        order.AssignTracking(TrackingId.From("TrackingId"));
        order.MarkEventsAsCommited();

        _orderRepository.GetByIdAsync(OrderId.From(data.CorrelationId), Arg.Any<CancellationToken>())
            .Returns(order);
        
        await _step.CompensateAsync(data, context, CancellationToken.None);
        

        await _shippingGateway.Received(1).CancelShipmentAsync(
            context.ShipmentId,
            Arg.Is<string>(s => s.Contains("saga compensation")),
            Arg.Any<CancellationToken>());
        
        Assert.Null(order.TrackingId);
        await _orderRepository.Received(1).AddAsync(order, Arg.Any<CancellationToken>());

        await _outboxRepository.Received(1).AddAsync(
            Arg.Any<Guid>(),
            Arg.Is<string>(s => s.Contains("OrderTrackingRemovedEvent")),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldDoNothing_whenShipmentIdIsEmpty()
    {
        var context = new OrderSagaContext { ShipmentId = null };

        await _step.CompensateAsync(CreateSampleData(), context, CancellationToken.None);

        await _shippingGateway.DidNotReceiveWithAnyArgs().CancelShipmentAsync(default!, default!, default);
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldRevertTracking_EvenIfGatewayFailsCancellation()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext { ShipmentId = "SHIP-FAIL" };

        _shippingGateway.CancelShipmentAsync(default!, default!, default)
            .Throws(new Exception("Shipping API Down"));

        var order = CreatePaidOrder(data.CorrelationId);
        order.AssignTracking(TrackingId.From("TrackingId"));
        _orderRepository.GetByIdAsync(OrderId.From(data.CorrelationId), Arg.Any<CancellationToken>())
            .Returns(order);

        var exception = await Record.ExceptionAsync(() =>
            _step.CompensateAsync(data, context, CancellationToken.None));

        Assert.Null(exception);

        await _orderRepository.Received(1).AddAsync(order, Arg.Any<CancellationToken>());

        Assert.Null(order.TrackingId);

        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // helpers

    private OrderSagaData CreateSampleData()
    {
        return new OrderSagaData
        {
            CorrelationId = Guid.NewGuid(),
            DeliveryAddress = new AddressDto("Streetko", "Citka", "Cunt_ry", "01091939"),
            Items = new List<OrderItemDto>
            {
                new OrderItemDto(Guid.NewGuid(), 1, 100, "USD")
            }
        };
    }

    private Order CreatePaidOrder(Guid orderId)
    {
        // valid order in paid status

        var customerId = CustomerId.CreateUnique();
        var address = Address.Create("Test St", "City", "Country", "00000");
        var items = new List<OrderItem>
        {
            OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD"))
        };

        var order = Order.Create(customerId, address, items);
        
        order.Pay(PaymentId.From("PAY-123"));
        
        order.MarkEventsAsCommited();
        
        typeof(Entity<OrderId>).GetProperty("Id")!
            .SetValue(order, OrderId.From(orderId));

        return order;
    }
}