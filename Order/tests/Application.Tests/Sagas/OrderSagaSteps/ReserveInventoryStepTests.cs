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

    private ReserveInventoryStep BuildStep() => new(_inventoryGateway, _logger);

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_AndSetReservationId_WhenGatewaySucceeds()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();
        var expectedReservationId = "RESERVATION-123";

        _inventoryGateway
            .ReserveAsync(data.CorrelationId, data.Items, Arg.Any<CancellationToken>())
            .Returns(expectedReservationId);

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(expectedReservationId, context.ReservationId);
        Assert.Equal(expectedReservationId, result.Data?["ReservationId"]);

        await _inventoryGateway.Received(1).ReserveAsync(
            data.CorrelationId, data.Items, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipGateway_WhenReservationIdAlreadyInContext_Idempotency()
    {
        var context = new OrderSagaContext { ReservationId = "EXISTING-RESERVATION" };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(true, result.Data?["Idempotent"]);

        await _inventoryGateway.DidNotReceive().ReserveAsync(
            Arg.Any<Guid>(), Arg.Any<List<OrderItemDto>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenInsufficientInventory()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();

        _inventoryGateway
            .ReserveAsync(Arg.Any<Guid>(), Arg.Any<List<OrderItemDto>>(), Arg.Any<CancellationToken>())
            .Throws(new InsufficientInventoryException("Out of stock"));

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Out of stock", result.ErrorMessage);
        Assert.Null(context.ReservationId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenUnexpectedExceptionOccurs()
    {
        _inventoryGateway
            .ReserveAsync(Arg.Any<Guid>(), Arg.Any<List<OrderItemDto>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Inventory service unreachable"));

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Inventory service unreachable", result.ErrorMessage);
    }

    [Fact]
    public async Task CompensateAsync_ShouldReleaseReservation_WhenReservationIdExists()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext { ReservationId = "RESERVATION-123" };

        await BuildStep().CompensateAsync(data, context, CancellationToken.None);

        await _inventoryGateway.Received(1).ReleaseReservationAsync(
            "RESERVATION-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotCallRelease_WhenReservationIdIsEmpty()
    {
        var context = new OrderSagaContext { ReservationId = null };

        await BuildStep().CompensateAsync(CreateSampleData(), context, CancellationToken.None);

        await _inventoryGateway.DidNotReceive().ReleaseReservationAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotThrow_WhenGatewayReleaseFails()
    {
        var context = new OrderSagaContext { ReservationId = "RESERVATION-123" };

        _inventoryGateway
            .ReleaseReservationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Inventory service down"));

        var exception = await Record.ExceptionAsync(() =>
            BuildStep().CompensateAsync(CreateSampleData(), context, CancellationToken.None));

        Assert.Null(exception);

        await _inventoryGateway.Received(1).ReleaseReservationAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
    
    private static OrderSagaData CreateSampleData() => new()
    {
        CorrelationId = Guid.NewGuid(),
        DeliveryAddress = new AddressDto("Baker St", "London", "UK", "NW1"),
        Items = new List<OrderItemDto> { new(Guid.NewGuid(), 2, 50m, "USD") }
    };
}