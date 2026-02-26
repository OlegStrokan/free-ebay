using Application.DTOs;
using Application.Gateways;
using Application.Interfaces;
using Application.Sagas.ReturnSaga;
using Application.Sagas.ReturnSaga.Steps;
using Domain.Entities;
using Domain.Entities.Order;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.ReturnSagaSteps;

public class ProcessRefundStepTests
{
    private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();
    private readonly IOrderPersistenceService _orderPersistenceService = Substitute.For<IOrderPersistenceService>();
    private readonly IReturnRequestPersistenceService _returnRequestPersistenceService =
        Substitute.For<IReturnRequestPersistenceService>();
    private readonly IIncidentReporter _incidentReporter = Substitute.For<IIncidentReporter>();
    private readonly ILogger<ProcessRefundStep> _logger = Substitute.For<ILogger<ProcessRefundStep>>();

    private ProcessRefundStep BuildStep() => new(
        _paymentGateway,
        _orderPersistenceService,
        _returnRequestPersistenceService,
        _incidentReporter,
        _logger);
    
    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenRefundAndPersistenceSucceed()
    {
        var order = CreateReturnReceivedOrder();
        var data = CreateSampleData();
        var context = new ReturnSagaContext();
        var expectedRefundId = "REF-999";

        _orderPersistenceService
            .LoadOrderAsync(data.CorrelationId, Arg.Any<CancellationToken>())
            .Returns(order);

        _paymentGateway
            .RefundAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedRefundId);

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(expectedRefundId, context.RefundId);
        Assert.Equal(expectedRefundId, result.Data?["RefundId"]);

        // original payment id PAY-1 must be used (we used it in CreateReturnReceivedOrder)
        await _paymentGateway.Received(1).RefundAsync(
            "PAY-1",
            data.RefundAmount,
            Arg.Is<string>(s => s.Contains(data.ReturnReason)),
            Arg.Any<CancellationToken>());
        
        await _returnRequestPersistenceService.Received(1).UpdateReturnRequestAsync(
            data.CorrelationId,
            Arg.Any<Func<ReturnRequest, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkip_WhenRefundIdAlreadyInContext_Idempotency()
    {
        var context = new ReturnSagaContext { RefundId = "EXISTING-REF" };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.True(result.Success);

        await _paymentGateway.DidNotReceive().RefundAsync(
            Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderNotFound()
    {
        var data = CreateSampleData();

        _orderPersistenceService
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);

        await _paymentGateway.DidNotReceive().RefundAsync(
            Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOrderHasNoPaymentId()
    {
        var unpaidOrder = Order.Create(
            CustomerId.CreateUnique(),
            Address.Create("A", "B", "C", "D"),
            new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD")) });
        // order.Pay() is NOT called

        var data = CreateSampleData();

        _orderPersistenceService
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(unpaidOrder);

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("no payment ID", result.ErrorMessage);

        await _paymentGateway.DidNotReceive().RefundAsync(
            Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPaymentGatewayFails()
    {
        var order = CreateReturnReceivedOrder();
        var data = CreateSampleData();

        _orderPersistenceService
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(order);

        _paymentGateway
            .RefundAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Payment Provider Unavailable"));

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Payment Provider Unavailable", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPersistenceUpdateFails()
    {
        var order = CreateReturnReceivedOrder();
        var data = CreateSampleData();

        _orderPersistenceService
            .LoadOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(order);

        _paymentGateway
            .RefundAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("REF-OK");

        _returnRequestPersistenceService
            .UpdateReturnRequestAsync(Arg.Any<Guid>(), Arg.Any<Func<ReturnRequest, Task>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Deadlock detected"));

        var result = await BuildStep().ExecuteAsync(data, new ReturnSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Deadlock detected", result.ErrorMessage);
    }

    [Fact]
    public async Task CompensateAsync_ShouldLogCritical_WhenRefundExists()
    {
        var data = CreateSampleData();
        var context = new ReturnSagaContext { RefundId = "REF-TO-COMPENSATE" };

        await BuildStep().CompensateAsync(data, context, CancellationToken.None);

        _logger.Received().Log(
            LogLevel.Critical,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("CRITICAL")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        // no actual re-charge - just alerts
        await _paymentGateway.DidNotReceive().RefundAsync(
            Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldDoNothing_WhenRefundIdIsEmpty()
    {
        var context = new ReturnSagaContext { RefundId = null };

        await BuildStep().CompensateAsync(CreateSampleData(), context, CancellationToken.None);

        _logger.DidNotReceive().Log(
            LogLevel.Critical,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotThrow_WhenAlertingFails()
    {
        var context = new ReturnSagaContext { RefundId = "REF-123" };

        _logger.When(x => x.Log(
                LogLevel.Critical,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()))
            .Do(_ => throw new Exception("Alerting System Crash"));

        var exception = await Record.ExceptionAsync(() =>
            BuildStep().CompensateAsync(CreateSampleData(), context, CancellationToken.None));

        Assert.Null(exception);
    }
    
    private static ReturnSagaData CreateSampleData() => new()
    {
        CorrelationId = Guid.NewGuid(),
        RefundAmount = 100m,
        Currency = "USD",
        ReturnReason = "Defective Product",
        CustomerId = Guid.NewGuid(),
        ReturnedItems = new List<OrderItemDto>()
    };

    private static Order CreateReturnReceivedOrder()
    {
        var order = Order.Create(
            CustomerId.CreateUnique(),
            Address.Create("Street", "City", "Country", "12345"),
            new List<OrderItem> { OrderItem.Create(ProductId.CreateUnique(), 1, Money.Create(100, "USD")) });

        order.Pay(PaymentId.From("PAY-1"));
        order.Approve();
        order.Complete();
        order.ClearUncommittedEvents();
        return order;
    }
}