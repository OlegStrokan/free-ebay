using Application.DTOs;
using Application.Gateways;
using Application.Sagas.ReturnSaga;
using Application.Sagas.ReturnSaga.Steps;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.ReturnSagaSteps;

public sealed class AwaitReturnShipmentStepTests
{
    private readonly IShippingGateway _shippingGateway = Substitute.For<IShippingGateway>();
    private readonly ILogger<AwaitReturnShipmentStep> _logger = Substitute.For<ILogger<AwaitReturnShipmentStep>>();
    private readonly AwaitReturnShipmentStep _step;

    public AwaitReturnShipmentStepTests()
    {
        _step = new AwaitReturnShipmentStep(_shippingGateway, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenGatewaySucceeds()
    {
        var data = CreateSampleData();
        var context = new ReturnSagaContext();
        var expectedShipmentId = "RETURN-SHIP-999";

        _shippingGateway.CreateReturnShipmentAsync(
            data.CorrelationId,
            data.CustomerId,
            data.ReturnedItems,
            Arg.Any<CancellationToken>()).Returns(expectedShipmentId);

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.True(result.Success);
        Assert.Equal(expectedShipmentId, context.ReturnShipmentId);
        Assert.Equal(expectedShipmentId, result.Data?["ReturnShipmentId"]);

        await _shippingGateway.Received(1).CreateReturnShipmentAsync(
            data.CorrelationId,
            data.CustomerId,
            data.ReturnedItems,
            Arg.Any<CancellationToken>());

        await _shippingGateway.Received().RegisterWebhookAsync(
            expectedShipmentId,
            Arg.Any<string>(),
            Arg.Is<string[]>(events => events.Contains("return.delivered")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenShipmentCreationFails()
    {
        var data = CreateSampleData();
        var context = new ReturnSagaContext();
        var errorMessage = "Shipping API Unavailable";

        _shippingGateway.CreateReturnShipmentAsync(default, default, default!, default)
            .Throws(new Exception(errorMessage));

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(errorMessage, result.ErrorMessage);
        Assert.Null(context.ReturnShipmentId);

        await _shippingGateway.DidNotReceive().RegisterWebhookAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenWebhookRegistrationFails()
    {

        var exceptionMessage = "Webhook failed";
        
        var data = CreateSampleData();
        var shipmentId = "SHIP-OK";

        _shippingGateway.CreateReturnShipmentAsync(default, default, default!, default)
            .ReturnsForAnyArgs(shipmentId);

        _shippingGateway.RegisterWebhookAsync(default, default, default!, default)
            .Throws(new Exception(exceptionMessage));

        var result = await _step.ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(exceptionMessage, result.ErrorMessage);
    }
    
    // compensation tests

    [Fact]
    public async Task CompensationAsync_ShouldCancelShipment_WhenShipmentIdExists()
    {
        var data = CreateSampleData();
        var context = new ReturnSagaContext { ReturnShipmentId = "ReturnShipmentId" };

        await _step.CompensateAsync(data, context, CancellationToken.None);

        await _shippingGateway.Received(1).CancelReturnShipmentAsync(
            context.ReturnShipmentId,
            Arg.Is<string>(s => s.Contains("compensation")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldDoNothing_WhenShipmentIdIsEmpty()
    {
        var data = CreateSampleData();
        var context = new ReturnSagaContext() { ReturnShipmentId = null };

        await _step.CompensateAsync(data, context, CancellationToken.None);
        await _shippingGateway.DidNotReceiveWithAnyArgs().CancelReturnShipmentAsync(default!, default!, default);
        
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("No return shipment to cancel")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldCatchException_WhenGatewayFails()
    {
        var data = CreateSampleData();
        var context = new ReturnSagaContext { ReturnShipmentId = "ReturnShipmentId" };

        _shippingGateway.CancelReturnShipmentAsync(default!, default, default)
            .Throws(new Exception("Gateway Timeout"));
        
        // record.exceptionAsync used to prove that the method does not throw
        var exception = await Record.ExceptionAsync(() =>
            _step.CompensateAsync(data, context, CancellationToken.None));

        Assert.Null(exception);
        
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to cancel")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
    
    // helpers

    private ReturnSagaData CreateSampleData()
    {
        return new ReturnSagaData
        {
            CorrelationId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ReturnedItems = new List<OrderItemDto>
            {
                new OrderItemDto(Guid.NewGuid(), 1, 50, "USD")
            }
        };
    }
}