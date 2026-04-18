using Domain.Entities.UserRestriction;
using Domain.Repositories;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRestrictionRepository(AppDbContext dbContext) : IUserRestrictionRepository
{
    public async Task AddAsync(UserRestrictionEntity entity)
    {
        dbContext.UserRestrictions.Add(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task<UserRestrictionEntity?> GetActiveRestrictionAsync(string restrictedUserId)
    {
        return await dbContext.UserRestrictions
            .FirstOrDefaultAsync(r =>
                r.RestrictedUserId == restrictedUserId &&
                r.LiftedAt == null &&
                (r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow));
    }

    public async Task UpdateAsync(UserRestrictionEntity entity)
    {
        dbContext.UserRestrictions.Update(entity);
        await dbContext.SaveChangesAsync();
    }
}
