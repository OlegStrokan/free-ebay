using Application.Dtos;
using Domain.Entities.BlockedUser;
using Domain.Entities.User;
using Domain.Repositories;

namespace Application.UseCases.BlockUser;

public class BlockUserUseCase(
    IUserRepository userRepository,
    IBlockedUserRepository blockedUserRepository) : IBlockUserUseCase
{
    private static readonly HashSet<string> _allowedRoles = ["Admin", "Moderator"];

    public async Task<BlockUserResponse> ExecuteAsync(BlockUserCommand command)
    {
        if (command.TargetUserId == command.ActorUserId)
            throw new InvalidOperationException("User cannot block themselves");

        var actor = await userRepository.GetUserById(command.ActorUserId);
        if (actor == null)
            throw new KeyNotFoundException($"Actor user with ID {command.ActorUserId} not found");

        var actorRoles = actor.UserRoles.Select(ur => ur.Role.Name).ToHashSet();
        if (!actorRoles.Overlaps(_allowedRoles))
            throw new UnauthorizedAccessException("Only Admins and Moderators can block users");

        var target = await userRepository.GetUserById(command.TargetUserId);
        if (target == null)
            throw new KeyNotFoundException($"User with ID {command.TargetUserId} not found");

        if (target.Status == UserStatus.Blocked)
            throw new InvalidOperationException($"User {command.TargetUserId} is already blocked");

        target.Status = UserStatus.Blocked;
        var updatedUser = await userRepository.UpdateUser(target);

        var blockedRecord = new BlockedUserEntity
        {
            Id = Guid.NewGuid().ToString(),
            BlockedUserId = command.TargetUserId,
            BlockedById = command.ActorUserId,
            Reason = command.Reason,
            BlockedAt = DateTime.UtcNow,
        };
        await blockedUserRepository.AddAsync(blockedRecord);

        return new BlockUserResponse(
            updatedUser.Id,
            updatedUser.Email,
            updatedUser.Fullname,
            updatedUser.Phone,
            updatedUser.CountryCode,
            updatedUser.CustomerTier,
            updatedUser.Status,
            updatedUser.CreatedAt,
            updatedUser.UpdatedAt,
            BlockedById: command.ActorUserId,
            Reason: command.Reason,
            IsEmailVerified: updatedUser.IsEmailVerified,
            DeliveryInfos: updatedUser.DeliveryInfos.ToDtos(),
            Roles: updatedUser.UserRoles.Select(ur => ur.Role.Name).ToList());
    }
}
