using Domain.Entities.User;
using Domain.Repositories;
using Infrastructure.DbContext;

namespace Infrastructure.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<UserEntity?> GetUserById(string id)
    {
        return await dbContext.FindAsync<UserEntity>(id);

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
            throw new InvalidOperationException($"User with id {id} not found");
        }

        dbContext.Remove(user);
        await dbContext.SaveChangesAsync();
        }
}