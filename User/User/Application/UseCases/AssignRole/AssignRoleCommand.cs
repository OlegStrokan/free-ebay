namespace Application.UseCases.AssignRole;

public record AssignRoleCommand(string UserId, string RoleName, string AssignedBy);
