using Domain.Entities.BlockedUser;

namespace Domain.Repositories;

public interface IBlockedUserRepository
{
    Task AddAsync(BlockedUserEntity entity);
    Task<BlockedUserEntity?> GetActiveBlockAsync(string blockedUserId);
}
