using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.OrderSagaSteps;

public class ReserveInventoryStepTests
{
    private readonly IInventoryGateway _inventoryGateway = Substitute.For<IInventoryGateway>();
    private readonly ILogger<ReserveInventoryStep> _logger = Substitute.For<ILogger<ReserveInventoryStep>>();
    private readonly ReserveInventoryStep _step;

    public ReserveInventoryStepTests()
    {
        _step = new ReserveInventoryStep(_inventoryGateway, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenGatewaySucceeds()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();
        var expectedReservationId = "RESERVATION-123";

        _inventoryGateway.ReserveAsync(
            data.CorrelationId,
            data.Items,
            Arg.Any<CancellationToken>()).Returns(expectedReservationId);

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.True(result.Success);
        Assert.Equal(expectedReservationId, context.ReservationId);
        Assert.Equal(expectedReservationId, result.Data?["ReservationId"]);

        await _inventoryGateway.Received(1).ReserveAsync(
            data.CorrelationId, data.Items, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenAddressInvalid()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();
        var exceptionMessage = "Address out of delivery zone";

        _inventoryGateway.ReserveAsync(Arg.Any<Guid>(),
                Arg.Any<List<OrderItemDto>>(), Arg.Any<CancellationToken>())
            .Throws(new InsufficientInventoryException(exceptionMessage));

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains(exceptionMessage, result.ErrorMessage);
        Assert.Null(context.ReservationId);
    }
    
    

    [Fact]
    public async Task CompensateAsync_ShouldCallCancel_WhenReservationIdExists()
    {
        var data = CreateSampleData();
        var reservationId = "RESERVATION-123";
        var context = new OrderSagaContext { ReservationId = reservationId };

        await _step.CompensateAsync(data, context, CancellationToken.None);

        await _inventoryGateway.Received(1).ReleaseReservationAsync(
            reservationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotCallCancel_WhenReservationIdIsEmpty()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext { ReservationId = null };

        await _step.CompensateAsync(data, context, CancellationToken.None);

        await _inventoryGateway.DidNotReceive().ReleaseReservationAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
    
    
    private OrderSagaData CreateSampleData()
    {
        return new OrderSagaData
        {
            CorrelationId = Guid.NewGuid(),
            DeliveryAddress = new AddressDto("Antisimitskaya", "Adolfin", "Nazistan", "18000"),
            Items = new List<OrderItemDto>()
        };
    }
}