using Domain.Entities.UserRestriction;

namespace Domain.Repositories;

public interface IUserRestrictionRepository
{
    Task AddAsync(UserRestrictionEntity entity);
    Task<UserRestrictionEntity?> GetActiveRestrictionAsync(string restrictedUserId);
    Task UpdateAsync(UserRestrictionEntity entity);
}
