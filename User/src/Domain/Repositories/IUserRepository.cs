using System.Threading.Tasks;
using Domain.Entities.User;

namespace Domain.Repositories;

public interface IUserRepository
{
    Task<UserEntity?> GetUserById(string id);
    Task<UserEntity> CreateUser(UserEntity user);
    Task<UserEntity> UpdateUser(UserEntity user);
    Task DeleteUser(string id);
    
}  