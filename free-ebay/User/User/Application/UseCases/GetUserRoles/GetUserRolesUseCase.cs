using Domain.Repositories;

namespace Application.UseCases.GetUserRoles;

public class GetUserRolesUseCase(IUserRepository userRepository, IRoleRepository roleRepository) : IGetUserRolesUseCase
{
    public async Task<GetUserRolesResponse> ExecuteAsync(GetUserRolesQuery query)
    {
        var user = await userRepository.GetUserById(query.UserId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {query.UserId} not found");

        var roles = await roleRepository.GetUserRolesAsync(query.UserId);
        return new GetUserRolesResponse(roles.Select(r => r.Name).ToList());
    }
}
