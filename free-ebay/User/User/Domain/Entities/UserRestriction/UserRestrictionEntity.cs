using Domain.Entities.User;

namespace Domain.Entities.UserRestriction;

public class UserRestrictionEntity
{
    public required string Id { get; init; }
    public required string RestrictedUserId { get; init; }
    public required string RestrictedById { get; init; }
    public required RestrictionType Type { get; set; }
    public required string Reason { get; set; }
    public required DateTime RestrictedAt { get; init; }
    public DateTime? ExpiresAt { get; set; }     // null = permanent
    public DateTime? LiftedAt { get; set; }
    public string? LiftedById { get; set; }

    public UserEntity RestrictedUser { get; init; } = null!;
    public UserEntity RestrictedBy { get; init; } = null!;
}
