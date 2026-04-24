using System.Threading.Tasks;
using Domain.Entities.User;

namespace Domain.Repositories;

public interface IUserRepository
{
    // Both methods must include UserRoles → Role navigation
    Task<UserEntity?> GetUserById(string id);
    Task<UserEntity?> GetUserByEmail(string email);
    Task<bool> ExistsByEmail(string email);
    Task<UserEntity> CreateUser(UserEntity user);
    Task<UserEntity> UpdateUser(UserEntity user);
    Task DeleteUser(string id);
}