using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.OrderSagaSteps;

public class CreateShipmentStepTests
{
    private readonly IShippingGateway _shippingGateway = Substitute.For<IShippingGateway>();
    private readonly ILogger<CreateShipmentStep> _logger = Substitute.For<ILogger<CreateShipmentStep>>();
    private readonly CreateShipmentStep _step;

    public CreateShipmentStepTests()
    {
        _step = new CreateShipmentStep(_shippingGateway, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenGatewaySucceeds()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();
        var expectedShipmentId = "SHIP-123";

        _shippingGateway.CreateShipmentAsync(
                data.CorrelationId,
                data.DeliveryAddress,
                data.Items,
                Arg.Any<CancellationToken>())
            .Returns(expectedShipmentId);

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.True(result.Success);
        Assert.Equal(expectedShipmentId, context.ShipmentId);
        Assert.Equal(expectedShipmentId, result.Data?["ShipmentId"]);

        await _shippingGateway.Received(1).CreateShipmentAsync(
            data.CorrelationId,
            data.DeliveryAddress,
            data.Items,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenAddressIsValidInvalid()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();
        var exceptionMessage = "Address out of delivery zone";


        _shippingGateway.CreateShipmentAsync(Arg.Any<Guid>(), Arg.Any<AddressDto>(),
                Arg.Any<List<OrderItemDto>>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidAddressException(exceptionMessage));

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains(exceptionMessage, result.ErrorMessage);
        Assert.Null(context.ShipmentId);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenInvalidAddressException()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();
        var exceptionMessage = "Invalid Address";
        
        _shippingGateway.CreateShipmentAsync(Arg.Any<Guid>(), Arg.Any<AddressDto>(), 
                Arg.Any<List<OrderItemDto>>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidAddressException(exceptionMessage));

        var result = await _step.ExecuteAsync(data, new OrderSagaContext(), CancellationToken.None);
    
        Assert.False(result.Success);
        Assert.Contains(exceptionMessage, result.ErrorMessage);
        Assert.Null(context.ShipmentId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenGeneralExceptionOccurs()
    {
        var data = CreateSampleData();
        _shippingGateway.CreateShipmentAsync(Arg.Any<Guid>(), Arg.Any<AddressDto>(), 
                Arg.Any<List<OrderItemDto>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Database connection failed"));

        var result = await _step.ExecuteAsync(data, new OrderSagaContext(), CancellationToken.None);
    
        Assert.False(result.Success);
        Assert.Contains("Database connection failed", result.ErrorMessage);
    }
    

    [Fact]
    public async Task CompensateAsync_ShouldCallCancel_WhenShipmentIdExists()
    {
        var data = CreateSampleData();
        var shipmentId = "SHIP-123";
        var context = new OrderSagaContext { ShipmentId = shipmentId };

        await _step.CompensateAsync(data, context, CancellationToken.None);

        await _shippingGateway.Received(1).CancelShipmentAsync(
            shipmentId,
            Arg.Is<string>(s => s.Contains("saga compensation")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotCallCancel_WhenShipmentIdIsEmpty()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext { ShipmentId = null };

        await _step.CompensateAsync(data, context, CancellationToken.None);

        await _shippingGateway.DidNotReceive().CancelShipmentAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
    

    [Fact]
    public async Task CompensateAsync_ShouldCatchException_WhenGatewayFails()
    {
        var data = CreateSampleData();
        var shipmentId = "SHIP-123";
        var context = new OrderSagaContext { ShipmentId = shipmentId };

        _shippingGateway.CancelShipmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Shipping provider API is down"));

      
        var exception = await Record.ExceptionAsync(() =>
            _step.CompensateAsync(data, context, CancellationToken.None));

        Assert.Null(exception);

        await _shippingGateway.Received(1).CancelShipmentAsync(
            shipmentId,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private OrderSagaData CreateSampleData()
    {
        return new OrderSagaData
        {
            CorrelationId = Guid.NewGuid(),
            DeliveryAddress = new AddressDto("Street", "City", "Country", "12345"),
            Items = new List<OrderItemDto>()
        };
    }
}

