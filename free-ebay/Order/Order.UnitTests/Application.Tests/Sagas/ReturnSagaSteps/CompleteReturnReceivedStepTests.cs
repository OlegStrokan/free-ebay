using Application.DTOs;
using Application.Gateways;
using Application.Interfaces;
using Application.Sagas.ReturnSaga;
using Application.Sagas.ReturnSaga.Steps;
using Domain.Entities;
using Domain.Entities.RequestReturn;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.ReturnSagaSteps;

public class CompleteReturnReceivedStepTests
{
    private readonly IReturnRequestPersistenceService _returnRequestPersistenceService =
        Substitute.For<IReturnRequestPersistenceService>();
    private readonly IIncidentReporter _incidentReporter = Substitute.For<IIncidentReporter>();
    private readonly ILogger<CompleteReturnStep> _logger = Substitute.For<ILogger<CompleteReturnStep>>();

    private CompleteReturnStep BuildStep() => new(_returnRequestPersistenceService, _incidentReporter, _logger);
    
    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_AndCompleteReturnRequest()
    {
        var data = CreateSampleData();
        var context = new ReturnSagaContext { RefundId = "REF-123" };

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(data.CorrelationId, result.Data?["OrderId"]);
        Assert.Equal("Returned", result.Data?["FinalStatus"]);
        Assert.Equal("REF-123", result.Data?["RefundId"]);
        Assert.Equal(data.RefundAmount, result.Data?["RefundAmount"]);

        await _returnRequestPersistenceService.Received(1).UpdateReturnRequestAsync(
            data.CorrelationId,
            Arg.Any<Func<RequestReturn, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFallbackRefundId_WhenContextRefundIdIsNull()
    {
        var data = CreateSampleData();
        var context = new ReturnSagaContext { RefundId = null };

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("N/A", result.Data?["RefundId"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenUnexpectedExceptionOccurs()
    {
        var data = CreateSampleData();

        _returnRequestPersistenceService
            .UpdateReturnRequestAsync(Arg.Any<Guid>(), Arg.Any<Func<RequestReturn, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("DB connection lost"));

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("DB connection lost", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallUpdateWithCorrectOrderId()
    {
        var data = CreateSampleData();

        await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        await _returnRequestPersistenceService.Received(1).UpdateReturnRequestAsync(
            Arg.Is<Guid>(id => id == data.CorrelationId),
            Arg.Any<Func<RequestReturn, Task>>(),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task CompensateAsync_ShouldNotThrow_AsFinalStepWithManualReview()
    {
        // CompleteReturn is the last step - compensation is just a warning log, no state change
        var exception = await Record.ExceptionAsync(() =>
            BuildStep().CompensateAsync(CreateSampleData(), new ReturnSagaContext(), CancellationToken.None));

        Assert.Null(exception);

        await _returnRequestPersistenceService.DidNotReceive().UpdateReturnRequestAsync(
            Arg.Any<Guid>(), Arg.Any<Func<RequestReturn, Task>>(), Arg.Any<CancellationToken>());
    }
    
    private static ReturnSagaData CreateSampleData() => new()
    {
        CorrelationId = Guid.NewGuid(),
        RefundAmount = 500m,
        Currency = "USD",
        ReturnReason = "Customer changed mind",
        CustomerId = Guid.NewGuid(),
        ReturnedItems = new List<OrderItemDto> { new(Guid.NewGuid(), 1, 500m, "USD") }
    };
}