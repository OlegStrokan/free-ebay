using Application.Dtos;
using Domain.Entities.User;
using Domain.Entities.UserRestriction;
using Domain.Repositories;

namespace Application.UseCases.RestrictUser;

public class RestrictUserUseCase(
    IUserRepository userRepository,
    IUserRestrictionRepository restrictionRepository) : IRestrictUserUseCase
{
    private static readonly HashSet<string> _allowedRoles = ["Admin", "Moderator"];

    public async Task<RestrictUserResponse> ExecuteAsync(RestrictUserCommand command)
    {
        if (command.TargetUserId == command.ActorUserId)
            throw new InvalidOperationException("Actor cannot restrict themselves");

        var actor = await userRepository.GetUserById(command.ActorUserId);
        if (actor == null)
            throw new KeyNotFoundException($"Actor user with ID {command.ActorUserId} not found");

        var actorRoles = actor.UserRoles.Select(ur => ur.Role.Name).ToHashSet();
        if (!actorRoles.Overlaps(_allowedRoles))
            throw new UnauthorizedAccessException("Only Admins and Moderators can restrict users");

        var target = await userRepository.GetUserById(command.TargetUserId);
        if (target == null)
            throw new KeyNotFoundException($"User with ID {command.TargetUserId} not found");

        if (target.Status != UserStatus.Active)
            throw new InvalidOperationException($"User {command.TargetUserId} already has an active restriction");

        target.Status = command.Type == RestrictionType.Banned
            ? UserStatus.Banned
            : UserStatus.Restricted;

        var updatedUser = await userRepository.UpdateUser(target);

        var restriction = new UserRestrictionEntity
        {
            Id = Guid.NewGuid().ToString(),
            RestrictedUserId = command.TargetUserId,
            RestrictedById = command.ActorUserId,
            Type = command.Type,
            Reason = command.Reason,
            RestrictedAt = DateTime.UtcNow,
            ExpiresAt = command.ExpiresAt,
        };
        await restrictionRepository.AddAsync(restriction);

        return new RestrictUserResponse(
            updatedUser.Id,
            updatedUser.Email,
            updatedUser.Fullname,
            updatedUser.Phone,
            updatedUser.CountryCode,
            updatedUser.CustomerTier,
            updatedUser.Status,
            updatedUser.CreatedAt,
            updatedUser.UpdatedAt,
            RestrictedById: command.ActorUserId,
            RestrictionType: command.Type,
            Reason: command.Reason,
            ExpiresAt: command.ExpiresAt,
            IsEmailVerified: updatedUser.IsEmailVerified,
            DeliveryInfos: updatedUser.DeliveryInfos.ToDtos(),
            Roles: updatedUser.UserRoles.Select(ur => ur.Role.Name).ToList());
    }
}
