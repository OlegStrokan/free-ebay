using Domain.Entities.User;
using Domain.Repositories;

namespace Application.UseCases.BlockUser;

public class BlockUserUseCase(IUserRepository repository) : IBlockUserUseCase
{
    public async Task<BlockUserResponse> ExecuteAsync(BlockUserCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            throw new ArgumentException("User id is required", nameof(command.Id));
        }

        var user = await repository.GetUserById(command.Id);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {command.Id} not found");
        }

        user.Status = UserStatus.Blocked;

        var updatedUser = await repository.UpdateUser(user);
        return new BlockUserResponse(
            updatedUser.Id,
            updatedUser.Email,
            updatedUser.Fullname,
            updatedUser.Phone,
            updatedUser.CountryCode,
            updatedUser.CustomerTier,
            updatedUser.Status,
            updatedUser.CreatedAt,
            updatedUser.UpdatedAt,
            updatedUser.IsEmailVerified);
    }
}
