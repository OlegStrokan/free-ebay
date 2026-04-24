using Domain.Entities.User;
using Domain.Repositories;

namespace Application.UseCases.LiftRestriction;

public class LiftRestrictionUseCase(
    IUserRepository userRepository,
    IUserRestrictionRepository restrictionRepository) : ILiftRestrictionUseCase
{
    private static readonly HashSet<string> _allowedRoles = ["Admin", "Moderator"];

    public async Task<LiftRestrictionResponse> ExecuteAsync(LiftRestrictionCommand command)
    {
        var actor = await userRepository.GetUserById(command.ActorUserId);
        if (actor == null)
            throw new KeyNotFoundException($"Actor user with ID {command.ActorUserId} not found");

        var actorRoles = actor.UserRoles.Select(ur => ur.Role.Name).ToHashSet();
        if (!actorRoles.Overlaps(_allowedRoles))
            throw new UnauthorizedAccessException("Only Admins and Moderators can lift restrictions");

        var target = await userRepository.GetUserById(command.TargetUserId);
        if (target == null)
            throw new KeyNotFoundException($"User with ID {command.TargetUserId} not found");

        if (target.Status == UserStatus.Active)
            throw new InvalidOperationException($"User {command.TargetUserId} has no active restriction");

        var restriction = await restrictionRepository.GetActiveRestrictionAsync(command.TargetUserId);
        if (restriction != null)
        {
            restriction.LiftedAt = DateTime.UtcNow;
            restriction.LiftedById = command.ActorUserId;
            await restrictionRepository.UpdateAsync(restriction);
        }

        target.Status = UserStatus.Active;
        await userRepository.UpdateUser(target);

        return new LiftRestrictionResponse(Success: true);
    }
}
