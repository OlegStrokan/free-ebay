using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.OrderSagaSteps;

public class ProcessPaymentStepTests
{
    private readonly IPaymentGateway _paymentGateway = Substitute.For<IPaymentGateway>();
    private readonly IIncidentReporter _incidentReporter = Substitute.For<IIncidentReporter>();
    private readonly ILogger<ProcessPaymentStep> _logger = Substitute.For<ILogger<ProcessPaymentStep>>();

    private ProcessPaymentStep BuildStep() => new(_paymentGateway, _incidentReporter, _logger);

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_AndSetPaymentContext_WhenGatewaySucceeds()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();
        var expectedPaymentId = "PAYMENT_123";

        _paymentGateway.ProcessPaymentAsync(
                data.CorrelationId, data.CustomerId, data.TotalAmount,
                data.Currency, data.PaymentMethod, Arg.Any<CancellationToken>())
            .Returns(new PaymentProcessingResult(
                PaymentId: expectedPaymentId,
                Status: PaymentProcessingStatus.Succeeded,
                ProviderPaymentIntentId: "pi_123"));

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(expectedPaymentId, context.PaymentId);
        Assert.Equal(OrderSagaPaymentStatus.Succeeded, context.PaymentStatus);
        Assert.Equal("pi_123", context.ProviderPaymentIntentId);
        Assert.Equal(expectedPaymentId, result.Data?["PaymentId"]);

        await _paymentGateway.Received(1).ProcessPaymentAsync(
            data.CorrelationId, data.CustomerId, data.TotalAmount,
            data.Currency, data.PaymentMethod, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipGateway_WhenPaymentAlreadySucceeded_Idempotency()
    {
        var context = new OrderSagaContext
        {
            PaymentId = "EXISTING_PAY",
            PaymentStatus = OrderSagaPaymentStatus.Succeeded,
        };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.True(result.Success);

        await _paymentGateway.DidNotReceive().ProcessPaymentAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTreatLegacyPaymentIdAsSucceeded_WhenStatusNotStarted()
    {
        var context = new OrderSagaContext
        {
            PaymentId = "LEGACY_PAY",
            PaymentStatus = OrderSagaPaymentStatus.NotStarted,
        };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(OrderSagaPaymentStatus.Succeeded, context.PaymentStatus);

        await _paymentGateway.DidNotReceive().ProcessPaymentAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPauseSaga_WhenPaymentIsPending()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();

        _paymentGateway.ProcessPaymentAsync(
                data.CorrelationId, data.CustomerId, data.TotalAmount,
                data.Currency, data.PaymentMethod, Arg.Any<CancellationToken>())
            .Returns(new PaymentProcessingResult(
                PaymentId: "PAY-PENDING",
                Status: PaymentProcessingStatus.Pending,
                ProviderPaymentIntentId: "pi_pending"));

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(OrderSagaPaymentStatus.Pending, context.PaymentStatus);
        Assert.Equal("PAY-PENDING", context.PaymentId);
        Assert.True(result.Metadata.ContainsKey("SagaState"));
        Assert.Equal("WaitingForEvent", result.Metadata["SagaState"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPauseSaga_WhenPaymentRequiresAction()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();

        _paymentGateway.ProcessPaymentAsync(
                data.CorrelationId, data.CustomerId, data.TotalAmount,
                data.Currency, data.PaymentMethod, Arg.Any<CancellationToken>())
            .Returns(new PaymentProcessingResult(
                PaymentId: "PAY-3DS",
                Status: PaymentProcessingStatus.RequiresAction,
                ProviderPaymentIntentId: "pi_3ds",
                ClientSecret: "cs_3ds"));

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(OrderSagaPaymentStatus.RequiresAction, context.PaymentStatus);
        Assert.Equal("PAY-3DS", context.PaymentId);
        Assert.Equal("cs_3ds", context.PaymentClientSecret);
        Assert.True(result.Metadata.ContainsKey("SagaState"));
        Assert.Equal("WaitingForEvent", result.Metadata["SagaState"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPaymentDeclined()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();

        _paymentGateway.ProcessPaymentAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new PaymentDeclinedException("Card declined"));

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Card declined", result.ErrorMessage);
        Assert.Equal(OrderSagaPaymentStatus.Failed, context.PaymentStatus);
        Assert.Equal("PAYMENT_DECLINED", context.PaymentFailureCode);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenInsufficientFunds()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();

        _paymentGateway.ProcessPaymentAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InsufficientFundsException("Not enough balance"));

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Not enough balance", result.ErrorMessage);
        Assert.Equal(OrderSagaPaymentStatus.Failed, context.PaymentStatus);
        Assert.Equal("INSUFFICIENT_FUNDS", context.PaymentFailureCode);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenUnexpectedExceptionOccurs()
    {
        var context = new OrderSagaContext();

        _paymentGateway.ProcessPaymentAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Payment provider unreachable"));

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Payment provider unreachable", result.ErrorMessage);
        Assert.Equal(OrderSagaPaymentStatus.Failed, context.PaymentStatus);
    }

    [Fact]
    public async Task CompensateAsync_ShouldCallRefund_WhenPaymentSucceeded()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext
        {
            PaymentId = "PAY-123",
            PaymentStatus = OrderSagaPaymentStatus.Succeeded,
        };

        await BuildStep().CompensateAsync(data, context, CancellationToken.None);

        await _paymentGateway.Received(1).RefundAsync(
            "PAY-123",
            data.TotalAmount,
            Arg.Is<string>(s => s.Contains("saga compensation")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotCallRefund_WhenPaymentIdIsEmpty()
    {
        var context = new OrderSagaContext { PaymentId = null };

        await BuildStep().CompensateAsync(CreateSampleData(), context, CancellationToken.None);

        await _paymentGateway.DidNotReceive().RefundAsync(
            Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotCallRefund_WhenPaymentWasNotSucceeded()
    {
        var context = new OrderSagaContext
        {
            PaymentId = "PAY-PENDING",
            PaymentStatus = OrderSagaPaymentStatus.Pending,
        };

        await BuildStep().CompensateAsync(CreateSampleData(), context, CancellationToken.None);

        await _paymentGateway.DidNotReceive().RefundAsync(
            Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldRefundLegacyPayment_WhenStatusNotStartedButPaymentExists()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext
        {
            PaymentId = "LEGACY-PAY",
            PaymentStatus = OrderSagaPaymentStatus.NotStarted,
        };

        await BuildStep().CompensateAsync(data, context, CancellationToken.None);

        Assert.Equal(OrderSagaPaymentStatus.Succeeded, context.PaymentStatus);
        await _paymentGateway.Received(1).RefundAsync(
            "LEGACY-PAY",
            data.TotalAmount,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotThrow_WhenGatewayRefundFails()
    {
        var context = new OrderSagaContext
        {
            PaymentId = "PAY-123",
            PaymentStatus = OrderSagaPaymentStatus.Succeeded,
        };

        _paymentGateway.RefundAsync(
                Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Refund service down"));

        var exception = await Record.ExceptionAsync(() =>
            BuildStep().CompensateAsync(CreateSampleData(), context, CancellationToken.None));

        Assert.Null(exception);

        await _paymentGateway.Received(1).RefundAsync(
            Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }


    private static OrderSagaData CreateSampleData() => new()
    {
        CorrelationId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        TotalAmount = 150m,
        Currency = "USD",
        PaymentMethod = "CreditCard",
        DeliveryAddress = new AddressDto("Baker St", "London", "UK", "NW1"),
        Items = new List<OrderItemDto> { new(Guid.NewGuid(), 1, 150m, "USD") }
    };
}