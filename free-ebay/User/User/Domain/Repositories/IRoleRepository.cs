using Domain.Entities.Role;

namespace Domain.Repositories;

public interface IRoleRepository
{
    Task<IReadOnlyList<RoleEntity>> GetAllAsync();
    Task<RoleEntity?> GetByNameAsync(string name);
    Task<IReadOnlyList<RoleEntity>> GetUserRolesAsync(string userId);
    Task AssignRoleAsync(UserRoleEntity userRole);
    Task RevokeRoleAsync(string userId, string roleName);
}
