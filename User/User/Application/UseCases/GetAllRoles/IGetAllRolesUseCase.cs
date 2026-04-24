namespace Application.UseCases.GetAllRoles;

public interface IGetAllRolesUseCase
{
    Task<GetAllRolesResponse> ExecuteAsync();
}
