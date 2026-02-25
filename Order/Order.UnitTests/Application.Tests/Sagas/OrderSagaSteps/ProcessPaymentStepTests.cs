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
    private readonly ILogger<ProcessPaymentStep> _logger = Substitute.For<ILogger<ProcessPaymentStep>>();

    private ProcessPaymentStep BuildStep() => new(_paymentGateway, _logger);

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_AndSetPaymentIdInContext_WhenGatewaySucceeds()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();
        var expectedPaymentId = "PAYMENT_123";

        _paymentGateway.ProcessPaymentAsync(
                data.CorrelationId, data.CustomerId, data.TotalAmount,
                data.Currency, data.PaymentMethod, Arg.Any<CancellationToken>())
            .Returns(expectedPaymentId);

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(expectedPaymentId, context.PaymentId);
        Assert.Equal(expectedPaymentId, result.Data?["PaymentId"]);

        await _paymentGateway.Received(1).ProcessPaymentAsync(
            data.CorrelationId, data.CustomerId, data.TotalAmount,
            data.Currency, data.PaymentMethod, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipGateway_WhenPaymentIdAlreadyInContext_Idempotency()
    {
        var context = new OrderSagaContext { PaymentId = "EXISTING_PAY" };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(true, result.Data?["Idempotent"]);

        await _paymentGateway.DidNotReceive().ProcessPaymentAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
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
        Assert.Null(context.PaymentId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenInsufficientFunds()
    {
        var data = CreateSampleData();

        _paymentGateway.ProcessPaymentAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InsufficientFundsException("Not enough balance"));

        var result = await BuildStep().ExecuteAsync(data, new OrderSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Not enough balance", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenUnexpectedExceptionOccurs()
    {
        _paymentGateway.ProcessPaymentAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Payment provider unreachable"));

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Payment provider unreachable", result.ErrorMessage);
    }

    [Fact]
    public async Task CompensateAsync_ShouldCallRefund_WhenPaymentIdExists()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext { PaymentId = "PAY-123" };

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
    public async Task CompensateAsync_ShouldNotThrow_WhenGatewayRefundFails()
    {
        var context = new OrderSagaContext { PaymentId = "PAY-123" };

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