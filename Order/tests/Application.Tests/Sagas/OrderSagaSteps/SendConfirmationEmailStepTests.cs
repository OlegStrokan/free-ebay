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

    private readonly ILogger<SendConfirmationEmailStep> _logger =
        Substitute.For<ILogger<SendConfirmationEmailStep>>();

    private readonly SendConfirmationEmailStep _step;

    public SendConfirmationEmailStepTests()
    {
        _step = new SendConfirmationEmailStep(_emailGateway, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenEmailIsSent()
    {
        var data = CreateSampleSaga();
        var context = new OrderSagaContext();

        var result = await _step.ExecuteAsync(data, context, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True((bool?)result.Data?["EmailSent"]);

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
    public async Task ExecuteAsync_ShouldReturnSuccess_EvenWhenEmailGatewayFails()
    {
        var data = CreateSampleSaga();
        _emailGateway.SendOrderConfirmationAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<List<OrderItemDto>>(),
                Arg.Any<AddressDto>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("SMTP Server Down"));

        var result = await _step.ExecuteAsync(data, new OrderSagaContext(), CancellationToken.None);
        
        Assert.True(result.Success);
        Assert.False((bool?)result.Data?["EmailSent"]);
        Assert.Equal("Email failed but order is complete", result.Data?["Warning"]);
    }

    [Fact]
    public async Task CompensateAsync_ShouldOnlyLog_AsNoCompensationIsRequired()
    {
        var data = CreateSampleSaga();

        await _step.CompensateAsync(data, new OrderSagaContext(), CancellationToken.None);

        await _emailGateway.DidNotReceiveWithAnyArgs().SendOrderConfirmationAsync(
            default, default, default, default, default, default, default, CancellationToken.None);
    }

    private OrderSagaData CreateSampleSaga()
    {
        return new OrderSagaData
        {
            CorrelationId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 99.99m,
            Currency = "USD",
            DeliveryAddress = new AddressDto("Street", "City", "Country", "12345"),
            Items = new List<OrderItemDto> { new OrderItemDto(Guid.NewGuid(), 2, 99.99m, "USD") },
            PaymentMethod = "CreditCard"
        };
    }
}