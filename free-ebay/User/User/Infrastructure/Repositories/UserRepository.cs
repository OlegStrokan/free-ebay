using Domain.Entities.User;
using Domain.Repositories;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<UserEntity?> GetUserById(string id)
    {
        return await dbContext.Users
            .Include(u => u.DeliveryInfos)
            .FirstOrDefaultAsync(x => x.Id == id);

    }

    public async Task<UserEntity?> GetUserByEmail(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await dbContext.Users
            .Include(u => u.DeliveryInfos)
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail);
    }

    public async Task<bool> ExistsByEmail(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await dbContext.Users.AnyAsync(x => x.Email == normalizedEmail);

    }

    public async  Task<UserEntity> CreateUser(UserEntity user)
    {
        dbContext.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<UserEntity> UpdateUser(UserEntity user)
    {
        dbContext.Update(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    public async Task DeleteUser(string id)
    {
        var user = await GetUserById(id);
        if (user == null)
        {
            return;
        }

        dbContext.Remove(user);
        await dbContext.SaveChangesAsync();
    }
}
