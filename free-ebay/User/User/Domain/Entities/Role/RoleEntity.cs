namespace Domain.Entities.Role;

public class RoleEntity
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsSystem { get; set; } = true;

    public List<UserRoleEntity> UserRoles { get; set; } = [];
}
