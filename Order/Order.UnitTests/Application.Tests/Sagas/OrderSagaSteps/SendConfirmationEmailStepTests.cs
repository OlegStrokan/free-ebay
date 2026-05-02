using Application.Sagas.Steps;
using Application.DTOs;
using Application.Gateways;
using Application.Interfaces;
using Application.Sagas.OrderSaga;
using Application.Sagas.OrderSaga.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.Tests.Sagas.OrderSagaSteps;

public class SendConfirmationEmailStepTests
{
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IDeadLetterRepository _deadLetterRepository = Substitute.For<IDeadLetterRepository>();
    private readonly ILogger<SendConfirmationEmailStep> _logger = Substitute.For<ILogger<SendConfirmationEmailStep>>();

    private SendConfirmationEmailStep BuildStep() => new(_emailGateway, _deadLetterRepository, _logger);

    [Fact]
    public async Task ExecuteAsync_ShouldReturnCompleted_WithEmailSentTrue_WhenGatewaySucceeds()
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
    public async Task ExecuteAsync_ShouldRetryThreeTimes_BeforeGivingUp()
    {
        _emailGateway.SendOrderConfirmationAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<List<OrderItemDto>>(), Arg.Any<AddressDto>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("SMTP server down"));

        await BuildStep().ExecuteAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None);

        await _emailGateway.Received(3).SendOrderConfirmationAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
            Arg.Any<List<OrderItemDto>>(), Arg.Any<AddressDto>(), Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnCompleted_WithEmailSentFalse_WhenAllRetriesFail()
    {
        _emailGateway.SendOrderConfirmationAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<List<OrderItemDto>>(), Arg.Any<AddressDto>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("SMTP server down"));

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None);

        Assert.IsType<Completed>(result);
        Assert.Equal(false, ((Completed)result).Data?["EmailSent"]);
        Assert.Contains("dead-letter", ((Completed)result).Data?["Warning"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldWriteToDeadLetterRepository_WhenAllRetriesFail()
    {
        var data = CreateSampleData();
        _emailGateway.SendOrderConfirmationAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<List<OrderItemDto>>(), Arg.Any<AddressDto>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("SMTP server down"));

        await BuildStep().ExecuteAsync(data, new OrderSagaContext(), CancellationToken.None);

        await _deadLetterRepository.Received(1).AddAsync(
            messageId: Arg.Any<Guid>(),
            type: "EmailConfirmationFailed",
            content: Arg.Any<string>(),
            occuredOn: Arg.Any<DateTime>(),
            failureReason: Arg.Any<string>(),
            retryCount: 3,
            aggregateId: data.CorrelationId.ToString(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotWriteToDeadLetter_WhenFirstAttemptSucceeds()
    {
        await BuildStep().ExecuteAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None);

        await _deadLetterRepository.DidNotReceiveWithAnyArgs().AddAsync(
            default, default!, default!, default, default!, default, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSucceedOnSecondAttempt_WhenFirstFails()
    {
        var callCount = 0;
        _emailGateway.SendOrderConfirmationAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<List<OrderItemDto>>(), Arg.Any<AddressDto>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (++callCount == 1) throw new Exception("transient failure");
                return Task.CompletedTask;
            });

        var result = await BuildStep().ExecuteAsync(CreateSampleData(), new OrderSagaContext(), CancellationToken.None);

        Assert.IsType<Completed>(result);
        Assert.Equal(true, ((Completed)result).Data?["EmailSent"]);
        await _deadLetterRepository.DidNotReceiveWithAnyArgs().AddAsync(
            default, default!, default!, default, default!, default, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRethrow_WhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _emailGateway.SendOrderConfirmationAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<List<OrderItemDto>>(), Arg.Any<AddressDto>(), Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            BuildStep().ExecuteAsync(CreateSampleData(), new OrderSagaContext(), cts.Token));
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
        PaymentMethod = Application.Common.Enums.PaymentMethod.CreditCard
    };
}