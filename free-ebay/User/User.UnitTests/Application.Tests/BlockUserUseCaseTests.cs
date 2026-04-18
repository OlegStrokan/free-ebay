using Application.UseCases.BlockUser;
using Domain.Entities.BlockedUser;
using Domain.Entities.Role;
using Domain.Entities.User;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class BlockUserUseCaseTests
{
    private static UserEntity MakeUser(string id, string roleName)
    {
        var role = new RoleEntity { Id = $"role-{roleName.ToLower()}", Name = roleName, Description = roleName, IsSystem = true };
        return new UserEntity
        {
            Id = id,
            Email = $"{id}@example.com",
            Password = "hashed",
            Fullname = "Test User",
            Phone = "+1234567890",
            CountryCode = "DE",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserRoles =
            [
                new UserRoleEntity { UserId = id, RoleId = role.Id, AssignedBy = "system", AssignedAt = DateTime.UtcNow, Role = role },
            ],
        };
    }

    private static UserEntity MakeUser(string id) =>
        new()
        {
            Id = id,
            Email = $"{id}@example.com",
            Password = "hashed",
            Fullname = "Test User",
            Phone = "+1234567890",
            CountryCode = "DE",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    [Fact]
    public async Task ShouldBlockUser_WhenActorIsAdmin()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var blockedUserRepository = Substitute.For<IBlockedUserRepository>();

        var actor = MakeUser("actor-id", "Admin");
        var target = MakeUser("target-id");
        var command = new BlockUserCommand("target-id", "actor-id", "Violated ToS");

        userRepository.GetUserById("actor-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns(target);
        userRepository.UpdateUser(Arg.Any<UserEntity>()).Returns(callInfo => callInfo.Arg<UserEntity>());

        var useCase = new BlockUserUseCase(userRepository, blockedUserRepository);
        var result = await useCase.ExecuteAsync(command);

        Assert.Equal(UserStatus.Blocked, result.Status);
        Assert.Equal("actor-id", result.BlockedById);
        Assert.Equal("Violated ToS", result.Reason);
        Assert.Equal("target-id", result.Id);

        await blockedUserRepository.Received(1).AddAsync(
            Arg.Is<BlockedUserEntity>(b =>
                b.BlockedUserId == "target-id" &&
                b.BlockedById == "actor-id" &&
                b.Reason == "Violated ToS"));

        await userRepository.Received(1).UpdateUser(
            Arg.Is<UserEntity>(u => u.Id == "target-id" && u.Status == UserStatus.Blocked));
    }

    [Fact]
    public async Task ShouldBlockUser_WhenActorIsModerator()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var blockedUserRepository = Substitute.For<IBlockedUserRepository>();

        var actor = MakeUser("moderator-id", "Moderator");
        var target = MakeUser("target-id");
        var command = new BlockUserCommand("target-id", "moderator-id", "Spam");

        userRepository.GetUserById("moderator-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns(target);
        userRepository.UpdateUser(Arg.Any<UserEntity>()).Returns(callInfo => callInfo.Arg<UserEntity>());

        var useCase = new BlockUserUseCase(userRepository, blockedUserRepository);
        var result = await useCase.ExecuteAsync(command);

        Assert.Equal(UserStatus.Blocked, result.Status);
        Assert.Equal("Spam", result.Reason);
    }

    [Fact]
    public async Task ShouldThrow_WhenActorTriesToBlockThemselves()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var blockedUserRepository = Substitute.For<IBlockedUserRepository>();
        var command = new BlockUserCommand("same-id", "same-id", "reason");

        var useCase = new BlockUserUseCase(userRepository, blockedUserRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(command));

        await userRepository.DidNotReceive().GetUserById(Arg.Any<string>());
    }

    [Fact]
    public async Task ShouldThrow_WhenActorHasNoPrivilegedRole()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var blockedUserRepository = Substitute.For<IBlockedUserRepository>();

        var actor = MakeUser("actor-id", "User"); // plain User role
        var target = MakeUser("target-id");
        var command = new BlockUserCommand("target-id", "actor-id", "reason");

        userRepository.GetUserById("actor-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns(target);

        var useCase = new BlockUserUseCase(userRepository, blockedUserRepository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(command));

        await blockedUserRepository.DidNotReceive().AddAsync(Arg.Any<BlockedUserEntity>());
    }

    [Fact]
    public async Task ShouldThrow_WhenActorHasNoRolesAtAll()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var blockedUserRepository = Substitute.For<IBlockedUserRepository>();

        var actor = MakeUser("actor-id"); // no UserRoles
        var command = new BlockUserCommand("target-id", "actor-id", "reason");

        userRepository.GetUserById("actor-id").Returns(actor);

        var useCase = new BlockUserUseCase(userRepository, blockedUserRepository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ShouldThrow_WhenActorNotFound()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var blockedUserRepository = Substitute.For<IBlockedUserRepository>();
        var command = new BlockUserCommand("target-id", "actor-id", "reason");

        userRepository.GetUserById("actor-id").Returns((UserEntity?)null);

        var useCase = new BlockUserUseCase(userRepository, blockedUserRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ShouldThrow_WhenTargetNotFound()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var blockedUserRepository = Substitute.For<IBlockedUserRepository>();

        var actor = MakeUser("actor-id", "Admin");
        var command = new BlockUserCommand("target-id", "actor-id", "reason");

        userRepository.GetUserById("actor-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns((UserEntity?)null);

        var useCase = new BlockUserUseCase(userRepository, blockedUserRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => useCase.ExecuteAsync(command));

        await blockedUserRepository.DidNotReceive().AddAsync(Arg.Any<BlockedUserEntity>());
    }

    [Fact]
    public async Task ShouldThrow_WhenTargetIsAlreadyBlocked()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var blockedUserRepository = Substitute.For<IBlockedUserRepository>();

        var actor = MakeUser("actor-id", "Admin");
        var target = MakeUser("target-id");
        target.Status = UserStatus.Blocked;
        var command = new BlockUserCommand("target-id", "actor-id", "reason");

        userRepository.GetUserById("actor-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns(target);

        var useCase = new BlockUserUseCase(userRepository, blockedUserRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(command));

        await userRepository.DidNotReceive().UpdateUser(Arg.Any<UserEntity>());
        await blockedUserRepository.DidNotReceive().AddAsync(Arg.Any<BlockedUserEntity>());
    }
}
