namespace Application.UseCases.GetUserRoles;

public interface IGetUserRolesUseCase
{
    Task<GetUserRolesResponse> ExecuteAsync(GetUserRolesQuery query);
}
