using Domain.Entities.UserRestriction;

namespace Application.UseCases.RestrictUser;

public record RestrictUserCommand(
    string TargetUserId,
    string ActorUserId,
    RestrictionType Type,
    string Reason,
    DateTime? ExpiresAt);
