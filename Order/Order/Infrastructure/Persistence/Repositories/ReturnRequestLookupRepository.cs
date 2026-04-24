using Application.Interfaces;
using Domain.Entities;
using Domain.Entities.RequestReturn;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ReturnRequestLookupRepository(AppDbContext dbContext) : IReturnRequestLookupRepository
{
    public Task AddAsync(Guid orderId, Guid returnRequestId, CancellationToken cancellationToken)
    {
        dbContext.ReturnRequestLookups.Add(RequestReturnLookup.Create(orderId, returnRequestId));
        return Task.CompletedTask;
    }

    public async Task<Guid?> GetReturnRequestIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var lookup = await dbContext.ReturnRequestLookups
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);

        return lookup?.ReturnRequestId;
    }
}
