using Application.Sagas.Steps;
using Application.DTOs;
using Application.Gateways;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.OrderSagaSteps;

public class SendConfirmationEmailStepTests
{
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly ILogger<SendConfirmationEmailStep> _logger = Substitute.For<ILogger<SendConfirmationEmailStep>>();

    private SendConfirmationEmailStep BuildStep() => new(_emailGateway, _logger);

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_AndEmailSentTrue_WhenGatewaySucceeds()
    {
        var data = CreateSampleData();
        var context = new OrderSagaContext();

        var result = await BuildStep().ExecuteAsync(data, context, CancellationToken.None);

        Assert.IsType<Completed>(result);
        Assert.Equal(true, ((Completed)result).Data?["EmailSent"]);

        await _emailGateway.Received(1).SendOrderConfirmationAsync(
            data.CustomerId,
            data.CorrelationId,
            data.TotalAmount,
            data.Currency,
            data.Items,
            data.DeliveryAddress,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WithEmailSentFalse_WhenGatewayFails()
    {
        _emailGateway.SendOrderConfirmationAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<List<OrderItemDto>>(), Arg.Any<AddressDto>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("SMTP server down"));

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None);

        Assert.IsType<Completed>(result);
        Assert.Equal(false, ((Completed)result).Data?["EmailSent"]);
        Assert.Equal("Email failed but order is complete", ((Completed)result).Data?["Warning"]);
    }

    [Fact]
    public async Task CompensateAsync_ShouldNotSendAnything_AsEmailIsNotReversible()
    {
        await BuildStep().CompensateAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None);

        await _emailGateway.DidNotReceiveWithAnyArgs().SendOrderConfirmationAsync(
            default, default, default, default!, default!, default!, default, CancellationToken.None);
    }
    
    private static OrderSagaData CreateSampleData() => new()
    {
        CorrelationId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        TotalAmount = 99.99m,
        Currency = "USD",
        DeliveryAddress = new AddressDto("Baker St", "London", "UK", "NW1"),
        Items = new List<OrderItemDto> { new(Guid.NewGuid(), 2, 99.99m, "USD") },
        PaymentMethod = "CreditCard"
    };
}