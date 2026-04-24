namespace Application.UseCases.GetUserRoles;

public record GetUserRolesResponse(IReadOnlyList<string> RoleNames);
