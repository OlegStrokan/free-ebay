using Domain.Repositories;

namespace Application.UseCases.RevokeRole;

public class RevokeRoleUseCase(IUserRepository userRepository, IRoleRepository roleRepository) : IRevokeRoleUseCase
{
    public async Task<RevokeRoleResponse> ExecuteAsync(RevokeRoleCommand command)
    {
        var user = await userRepository.GetUserById(command.UserId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {command.UserId} not found");

        var hasRole = user.UserRoles.Any(ur => ur.Role.Name == command.RoleName);
        if (!hasRole)
            throw new InvalidOperationException($"User {command.UserId} does not have role '{command.RoleName}'");

        await roleRepository.RevokeRoleAsync(command.UserId, command.RoleName);
        return new RevokeRoleResponse(true);
    }
}
