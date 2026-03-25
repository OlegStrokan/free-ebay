using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.UnitOfWork;

public class EfUnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChanges_AndReturnAffectedRows()
    {
        await using var context = Persistence.TestDbContextFactory.Create();
        var unitOfWork = new EfUnitOfWork(context);

        var payment = Payment.Create(
            PaymentId.From("pay-uow-1"),
            "order-uow-1",
            "customer-uow-1",
            Money.Create(99m, "USD"),
            PaymentMethod.Card,
            IdempotencyKey.From("idem-uow-1"));

        await context.Payments.AddAsync(payment);

        var affected = await unitOfWork.SaveChangesAsync();

        Assert.True(affected > 0);
        Assert.Equal(1, await context.Payments.CountAsync());
    }
}
