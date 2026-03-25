using Domain.Exceptions;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Infrastructure.Tests.Persistence.UnitOfWork;

public class EfUnitOfWorkExceptionTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldTranslateUniqueViolationToDomainException()
    {
        var postgres = new PostgresException(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation,
            detail: null,
            hint: null,
            position: 0,
            internalPosition: 0,
            internalQuery: null,
            where: null,
            schemaName: "public",
            tableName: "payments",
            columnName: null,
            dataTypeName: null,
            constraintName: "IX_payments_order_id_process_idempotency_key",
            file: "nbtinsert.c",
            line: "406",
            routine: "_bt_check_unique");

        var dbUpdateException = new DbUpdateException("db update failed", postgres);

        await using var context = CreateContextThatThrows(dbUpdateException);
        await context.Payments.AddAsync(CreatePayment("order-unique"));

        var unitOfWork = new EfUnitOfWork(context);

        var ex = await Assert.ThrowsAsync<UniqueConstraintViolationException>(() => unitOfWork.SaveChangesAsync());

        Assert.Equal("IX_payments_order_id_process_idempotency_key", ex.ConstraintName);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldRethrowOriginalException_WhenNotUniqueViolation()
    {
        var postgres = new PostgresException(
            messageText: "syntax error",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: "42601",
            detail: null,
            hint: null,
            position: 0,
            internalPosition: 0,
            internalQuery: null,
            where: null,
            schemaName: null,
            tableName: null,
            columnName: null,
            dataTypeName: null,
            constraintName: null,
            file: "scan.l",
            line: "1234",
            routine: "scanner_yyerror");

        var dbUpdateException = new DbUpdateException("db update failed", postgres);

        await using var context = CreateContextThatThrows(dbUpdateException);
        await context.Payments.AddAsync(CreatePayment("order-non-unique"));

        var unitOfWork = new EfUnitOfWork(context);

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
    }

    private static PaymentDbContext CreateContextThatThrows(Exception ex)
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new ThrowOnSaveChangesInterceptor(ex))
            .Options;

        var context = new PaymentDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static Domain.Entities.Payment CreatePayment(string orderId)
    {
        return Domain.Entities.Payment.Create(
            PaymentId.From(Guid.NewGuid().ToString("N")),
            orderId,
            "customer-1",
            Domain.ValueObjects.Money.Create(10m, "USD"),
            Domain.Enums.PaymentMethod.Card,
            IdempotencyKey.From(Guid.NewGuid().ToString("N")));
    }

    private sealed class ThrowOnSaveChangesInterceptor(Exception toThrow) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            throw toThrow;
        }
    }
}
