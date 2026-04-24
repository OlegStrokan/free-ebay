using Domain.Repositories;

namespace Application.UseCases.GetAllRoles;

public class GetAllRolesUseCase(IRoleRepository roleRepository) : IGetAllRolesUseCase
{
    public async Task<GetAllRolesResponse> ExecuteAsync()
    {
        var roles = await roleRepository.GetAllAsync();
        return new GetAllRolesResponse(roles.Select(r => r.Name).ToList());
    }
}
