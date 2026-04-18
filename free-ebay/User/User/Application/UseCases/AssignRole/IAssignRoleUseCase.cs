namespace Application.UseCases.AssignRole;

public interface IAssignRoleUseCase
{
    Task<AssignRoleResponse> ExecuteAsync(AssignRoleCommand command);
}
