namespace Gateway.Api.Contracts.Roles;

public sealed record CreateRoleRequest(string Name);
public sealed record UpdateRoleRequest(string Name);
public sealed record RoleResponse(string Id, string Name);
