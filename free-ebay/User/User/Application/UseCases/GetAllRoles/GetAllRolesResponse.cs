namespace Application.UseCases.GetAllRoles;

public record GetAllRolesResponse(IReadOnlyList<string> RoleNames);
