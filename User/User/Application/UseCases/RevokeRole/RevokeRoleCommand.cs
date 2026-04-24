namespace Application.UseCases.RevokeRole;

public record RevokeRoleCommand(string UserId, string RoleName);
