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
    private readonly ProcessPaymentStep _step;

    public ProcessPaymentStepTests()
    {
        _step = new ProcessPaymentStep(_paymentGateway, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenGatewaySucceeds()
    {
        var data = CreateSampleSaga();
        var context = new OrderSagaContext();
        var expectedPaymentId = "PAYMENT_123";

        _paymentGateway.ProcessPaymentAsync(
                data.CorrelationId,
                data.CustomerId,
                data.TotalAmount,
                data.Currency,
                data.PaymentMethod,
                Arg.Any<CancellationToken>())
            .Returns(expectedPaymentId);

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(expectedPaymentId, context.PaymentId);
        Assert.Equal(expectedPaymentId, result.Data?["PaymentId"]);

        await _paymentGateway.Received(1).ProcessPaymentAsync(
            data.CorrelationId, data.CustomerId, data.TotalAmount, data.Currency, data.PaymentMethod,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPaymentDeclined()
    {
        var data = CreateSampleSaga();
        var context = new OrderSagaContext();
        var exceptionMessage = "Payment has been declined because you live in EU, fucker";

        _paymentGateway.ProcessPaymentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new PaymentDeclinedException(exceptionMessage));

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains(exceptionMessage, result.ErrorMessage);
        Assert.Null(context.PaymentId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenInsufficientFunds()
    {
        var data = CreateSampleSaga();
        var context = new OrderSagaContext();
        var exceptionMessage = "You are broke;)";

        _paymentGateway.ProcessPaymentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InsufficientFundsException(exceptionMessage));

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains(exceptionMessage, result.ErrorMessage);
        Assert.Null(context.PaymentId);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenGeneralExceptionOccurs()
    {
        var data = CreateSampleSaga();
        var context = new OrderSagaContext();
        var exceptionMessage = "Really general exception. general as fuck";

        _paymentGateway.ProcessPaymentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception(exceptionMessage));

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains(exceptionMessage, result.ErrorMessage);
        Assert.Null(context.PaymentId);
    }

    [Fact]
    public async Task CompensateAsync_ShouldCallRefund_whenPaymentIdExists()
    {
        var data = CreateSampleSaga();
        var paymentId = "PAYMENT_123";
        var context = new OrderSagaContext { PaymentId = paymentId };

        await _step.CompensateAsync(data, context, CancellationToken.None);

        await _paymentGateway.Received(1).RefundAsync(
            paymentId: paymentId,
            amount: data.TotalAmount,
            reason: Arg.Is<string>(s => s.Contains("saga compensation")),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotCallRefund_WhenPaymentIdIsEmpty()
    {
        var data = CreateSampleSaga();
        var context = new OrderSagaContext { PaymentId = null };

        await _step.CompensateAsync(data, context, CancellationToken.None);

        await _paymentGateway.DidNotReceive().RefundAsync(
            Arg.Any<string>(),
            Arg.Any<decimal>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompensateAsync_ShouldCatchException_WhenGatewayFails()
    {
        var data = CreateSampleSaga();
        var context = new OrderSagaContext { PaymentId = "PAYMENT_123" };
        _paymentGateway.RefundAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("External Refund Service Down"));

        var exception = await Record.ExceptionAsync(() =>
            _step.CompensateAsync(data, context, CancellationToken.None));
        
        Assert.Null(exception);

        await _paymentGateway.Received(1).RefundAsync(
            Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

    }

    

    private OrderSagaData CreateSampleSaga()
    {
        return new OrderSagaData
        {
            CorrelationId = Guid.NewGuid(),
            DeliveryAddress = new AddressDto("Street", "City", "Country", "12345"),
            Items = new List<OrderItemDto>()
        };
    }
}