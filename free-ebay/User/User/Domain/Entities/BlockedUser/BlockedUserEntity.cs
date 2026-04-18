using Domain.Entities.User;

namespace Domain.Entities.BlockedUser;

public class BlockedUserEntity
{
    public required string Id { get; init; }
    public required string BlockedUserId { get; init; }
    public required string BlockedById { get; init; }
    public required string Reason { get; set; }
    public required DateTime BlockedAt { get; init; }

    public UserEntity BlockedUser { get; init; } = null!;
    public UserEntity BlockedBy { get; init; } = null!;
}
