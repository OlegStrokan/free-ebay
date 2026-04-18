using Domain.Entities.User;

namespace Domain.Entities.Role;

public class UserRoleEntity
{
    public required string UserId { get; init; }
    public required string RoleId { get; init; }
    public required string AssignedBy { get; init; }
    public required DateTime AssignedAt { get; init; }

    public UserEntity User { get; init; } = null!;
    public RoleEntity Role { get; init; } = null!;
}
