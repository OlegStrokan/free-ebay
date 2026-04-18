using Domain.Entities.Role;
using Domain.Repositories;

namespace Application.UseCases.AssignRole;

public class AssignRoleUseCase(IUserRepository userRepository, IRoleRepository roleRepository) : IAssignRoleUseCase
{
    public async Task<AssignRoleResponse> ExecuteAsync(AssignRoleCommand command)
    {
        var user = await userRepository.GetUserById(command.UserId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {command.UserId} not found");

        var role = await roleRepository.GetByNameAsync(command.RoleName);
        if (role == null)
            throw new ArgumentException($"Role '{command.RoleName}' does not exist", nameof(command.RoleName));

        var alreadyAssigned = user.UserRoles.Any(ur => ur.Role.Name == command.RoleName);
        if (alreadyAssigned)
            throw new InvalidOperationException($"User {command.UserId} already has role '{command.RoleName}'");

        var userRole = new UserRoleEntity
        {
            UserId = command.UserId,
            RoleId = role.Id,
            AssignedBy = command.AssignedBy,
            AssignedAt = DateTime.UtcNow,
        };

        await roleRepository.AssignRoleAsync(userRole);
        return new AssignRoleResponse(true);
    }
}
