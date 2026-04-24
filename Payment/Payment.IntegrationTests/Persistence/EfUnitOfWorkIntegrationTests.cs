using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Payment.IntegrationTests.Infrastructure;
using Xunit;

namespace Payment.IntegrationTests.Persistence;

[Collection("PaymentIntegration")]
public sealed class EfUnitOfWorkIntegrationTests
{
    private readonly IntegrationFixture _fixture;

    public EfUnitOfWorkIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChanges_ShouldThrowUniqueConstraintViolation_ForDuplicateOrderAndIdempotency()
    {
        await using var scope = _fixture.CreateScope();
        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var orderId = $"order-{Guid.NewGuid():N}";
        var idem = IdempotencyKey.From($"idem-{Guid.NewGuid():N}");

        var first = Domain.Entities.Payment.Create(
            PaymentId.From(Guid.NewGuid().ToString("N")),
            orderId,
            $"customer-{Guid.NewGuid():N}",
            Money.Create(10m, "USD"),
            PaymentMethod.Card,
            idem);

        var second = Domain.Entities.Payment.Create(
            PaymentId.From(Guid.NewGuid().ToString("N")),
            orderId,
            $"customer-{Guid.NewGuid():N}",
            Money.Create(20m, "USD"),
            PaymentMethod.Card,
            idem);

        await paymentRepository.AddAsync(first);
        await unitOfWork.SaveChangesAsync();

        await paymentRepository.AddAsync(second);

        await Assert.ThrowsAsync<UniqueConstraintViolationException>(() => unitOfWork.SaveChangesAsync());
    }
}
