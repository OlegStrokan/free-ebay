using Domain.Entities.Role;
using Domain.Repositories;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RoleRepository(AppDbContext dbContext) : IRoleRepository
{
    public async Task<IReadOnlyList<RoleEntity>> GetAllAsync()
    {
        return await dbContext.Roles.ToListAsync();
    }

    public async Task<RoleEntity?> GetByNameAsync(string name)
    {
        return await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<IReadOnlyList<RoleEntity>> GetUserRolesAsync(string userId)
    {
        return await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .ToListAsync();
    }

    public async Task AssignRoleAsync(UserRoleEntity userRole)
    {
        dbContext.UserRoles.Add(userRole);
        await dbContext.SaveChangesAsync();
    }

    public async Task RevokeRoleAsync(string userId, string roleName)
    {
        var userRole = await dbContext.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Role.Name == roleName);

        if (userRole == null)
            throw new KeyNotFoundException($"User {userId} does not have role '{roleName}'");

        dbContext.UserRoles.Remove(userRole);
        await dbContext.SaveChangesAsync();
    }
}
