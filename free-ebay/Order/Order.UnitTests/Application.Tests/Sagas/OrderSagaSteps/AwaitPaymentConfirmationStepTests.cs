using Application.DTOs;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Sagas.OrderSagaSteps;

public class AwaitPaymentConfirmationStepTests
{
    private readonly ILogger<AwaitPaymentConfirmationStep> _logger =
        Substitute.For<ILogger<AwaitPaymentConfirmationStep>>();

    private AwaitPaymentConfirmationStep BuildStep() => new(_logger);

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenPaymentSucceeded()
    {
        var context = new OrderSagaContext
        {
            PaymentId = "PAY-123",
            PaymentStatus = OrderSagaPaymentStatus.Succeeded,
        };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("PAY-123", result.Data?["PaymentId"]);
        Assert.False(result.Metadata.ContainsKey("SagaState"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPaymentSucceededButPaymentIdMissing()
    {
        var context = new OrderSagaContext
        {
            PaymentId = null,
            PaymentStatus = OrderSagaPaymentStatus.Succeeded,
        };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("PaymentId is missing", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPaymentFailed()
    {
        var context = new OrderSagaContext
        {
            PaymentStatus = OrderSagaPaymentStatus.Failed,
            PaymentFailureMessage = "Card declined",
        };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Payment failed: Card declined", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnWaitingMetadata_WhenPaymentPending()
    {
        var context = new OrderSagaContext
        {
            PaymentId = "PAY-PENDING",
            PaymentStatus = OrderSagaPaymentStatus.Pending,
        };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Metadata.ContainsKey("SagaState"));
        Assert.Equal("WaitingForEvent", result.Metadata["SagaState"]);
        Assert.Equal("PAY-PENDING", result.Data?["PaymentId"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnWaitingMetadata_WhenPaymentRequiresAction()
    {
        var context = new OrderSagaContext
        {
            PaymentId = "PAY-3DS",
            PaymentStatus = OrderSagaPaymentStatus.RequiresAction,
        };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Metadata.ContainsKey("SagaState"));
        Assert.Equal("WaitingForEvent", result.Metadata["SagaState"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenPaymentStatusUnknown()
    {
        var context = new OrderSagaContext
        {
            PaymentId = null,
            PaymentStatus = OrderSagaPaymentStatus.NotStarted,
        };

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), context, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Payment state is unknown", result.ErrorMessage);
    }


    [Fact]
    public async Task CompensateAsync_ShouldNotThrow()
    {
        var exception = await Record.ExceptionAsync(() =>
            BuildStep().CompensateAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None));

        Assert.Null(exception);
    }

    private static OrderSagaData CreateSampleData() => new()
    {
        CorrelationId = Guid.NewGuid(),
        DeliveryAddress = new AddressDto("Baker St", "London", "UK", "NW1"),
        Items = new List<OrderItemDto>(),
    };
}
