using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Persistence.DbContext;

namespace Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
    {
        return await dbContext.
    }
}

// @todo: i am dump fuck. how i could think about saving a fucking....entire order
// so it's will override entire entity fuck
// google it and rewrite completely this shit. we need to save every fucking events
// into some event sourcing order table