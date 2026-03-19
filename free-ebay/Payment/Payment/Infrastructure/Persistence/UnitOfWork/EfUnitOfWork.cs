using Application.Interfaces;
using Infrastructure.Persistence.DbContext;

namespace Infrastructure.Persistence.UnitOfWork;

internal sealed class EfUnitOfWork(PaymentDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}