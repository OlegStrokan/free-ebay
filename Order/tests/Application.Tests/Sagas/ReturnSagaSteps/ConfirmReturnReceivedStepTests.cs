using Application.DTOs;
using Application.Interfaces;
using Application.Sagas.ReturnSaga;
using Application.Sagas.ReturnSaga.Steps;
using Domain.Entities;
using Domain.Exceptions;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.ReturnSagaSteps;

public class ConfirmReturnReceivedStepTests
{
    private readonly IReturnRequestPersistenceService _returnRequestPersistenceService =
        Substitute.For<IReturnRequestPersistenceService>();

    private readonly ILogger<ConfirmReturnReceivedStep> _logger =
        Substitute.For<ILogger<ConfirmReturnReceivedStep>>();

    private ConfirmReturnReceivedStep BuildStep() =>
        new(_returnRequestPersistenceService, _logger);

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenReturnRequestUpdatedSuccessfully()
    {
        var data = CreateSampleData();
        var context = new ReturnSagaContext { ReturnShipmentId = "RETURN-SHIP-1" };

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("ReturnReceived", result.Data?["Status"]);
        Assert.NotNull(context.ReturnReceivedAt);

        await _returnRequestPersistenceService.Received(1).UpdateReturnRequestAsync(
            data.CorrelationId,
            Arg.Any<Func<ReturnRequest, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenReturnRequestNotFound()
    {
        var data = CreateSampleData();

        _returnRequestPersistenceService
            .UpdateReturnRequestAsync(Arg.Any<Guid>(), Arg.Any<Func<ReturnRequest, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new OrderNotFoundException(data.CorrelationId));

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenUnexpectedExceptionOccurs()
    {
        var data = CreateSampleData();

        _returnRequestPersistenceService
            .UpdateReturnRequestAsync(Arg.Any<Guid>(), Arg.Any<Func<ReturnRequest, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Database Error"));

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Database Error", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassCorrectIdToUpdateReturnRequest()
    {
        var data = CreateSampleData();

        await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        await _returnRequestPersistenceService.Received(1).UpdateReturnRequestAsync(
            Arg.Is<Guid>(id => id == data.CorrelationId),
            Arg.Any<Func<ReturnRequest, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotThrow_AsManualInterventionLogged()
    {
        //  items can't be unreceived, needs manual review type shit
        var exception = await Record.ExceptionAsync(() =>
            BuildStep().CompensateAsync(CreateSampleData(), new ReturnSagaContext(), CancellationToken.None));

        Assert.Null(exception);

        await _returnRequestPersistenceService.DidNotReceive().UpdateReturnRequestAsync(
            Arg.Any<Guid>(), Arg.Any<Func<ReturnRequest, Task>>(), Arg.Any<CancellationToken>());
    }

    private static ReturnSagaData CreateSampleData() => new()
    {
        CorrelationId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        ReturnReason = "Wrong size",
        RefundAmount = 100m,
        Currency = "USD",
        ReturnedItems = new List<OrderItemDto>()
    };
}