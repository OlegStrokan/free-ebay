using Domain.Entities.BlockedUser;
using Domain.Repositories;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BlockedUserRepository(AppDbContext dbContext) : IBlockedUserRepository
{
    public async Task AddAsync(BlockedUserEntity entity)
    {
        dbContext.BlockedUsers.Add(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task<BlockedUserEntity?> GetActiveBlockAsync(string blockedUserId)
    {
        return await dbContext.BlockedUsers
            .FirstOrDefaultAsync(b => b.BlockedUserId == blockedUserId);
    }
}
