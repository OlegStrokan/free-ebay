using Application.Queries.GetPaymentById;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Domain.ValueObjects;
using NSubstitute;

namespace Application.Tests.Queries;

public class GetPaymentByIdQueryHandlerTests
{
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();

    private GetPaymentByIdQueryHandler BuildHandler() => new(_paymentRepository);

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPaymentNotFound()
    {
        _paymentRepository.GetByIdAsync(Arg.Any<PaymentId>(), Arg.Any<CancellationToken>())
            .Returns((Payment?)null);

        var result = await BuildHandler().Handle(new GetPaymentByIdQuery("missing"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("was not found", result.Errors[0]);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedPayment_WhenFound()
    {
        var payment = Payment.Create(
            PaymentId.From("pay-1"),
            "order-1",
            "customer-1",
            Money.Create(10m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-1"));

        _paymentRepository.GetByIdAsync(Arg.Any<PaymentId>(), Arg.Any<CancellationToken>())
            .Returns(payment);

        var result = await BuildHandler().Handle(new GetPaymentByIdQuery("pay-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.PaymentId));
        Assert.Equal("order-1", result.Value.OrderId);
    }
}
