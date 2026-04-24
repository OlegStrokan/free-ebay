namespace Application.UseCases.RevokeRole;

public interface IRevokeRoleUseCase
{
    Task<RevokeRoleResponse> ExecuteAsync(RevokeRoleCommand command);
}
