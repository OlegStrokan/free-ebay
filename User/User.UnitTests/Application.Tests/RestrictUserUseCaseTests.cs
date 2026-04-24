using Application.UseCases.RestrictUser;
using Domain.Entities.Role;
using Domain.Entities.User;
using Domain.Entities.UserRestriction;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class RestrictUserUseCaseTests
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
    public async Task ShouldRestrictUser_WhenActorIsAdmin()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var restrictionRepository = Substitute.For<IUserRestrictionRepository>();

        var actor = MakeUser("actor-id", "Admin");
        var target = MakeUser("target-id");
        var command = new RestrictUserCommand("target-id", "actor-id", RestrictionType.Restricted, "Violated ToS", null);

        userRepository.GetUserById("actor-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns(target);
        userRepository.UpdateUser(Arg.Any<UserEntity>()).Returns(callInfo => callInfo.Arg<UserEntity>());

        var useCase = new RestrictUserUseCase(userRepository, restrictionRepository);
        var result = await useCase.ExecuteAsync(command);

        Assert.Equal(UserStatus.Restricted, result.Status);
        Assert.Equal("actor-id", result.RestrictedById);
        Assert.Equal("Violated ToS", result.Reason);
        Assert.Equal("target-id", result.Id);

        await restrictionRepository.Received(1).AddAsync(
            Arg.Is<UserRestrictionEntity>(r =>
                r.RestrictedUserId == "target-id" &&
                r.RestrictedById == "actor-id" &&
                r.Reason == "Violated ToS" &&
                r.Type == RestrictionType.Restricted));

        await userRepository.Received(1).UpdateUser(
            Arg.Is<UserEntity>(u => u.Id == "target-id" && u.Status == UserStatus.Restricted));
    }

    [Fact]
    public async Task ShouldBanUser_WhenTypeIsBanned()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var restrictionRepository = Substitute.For<IUserRestrictionRepository>();

        var actor = MakeUser("actor-id", "Admin");
        var target = MakeUser("target-id");
        var command = new RestrictUserCommand("target-id", "actor-id", RestrictionType.Banned, "Fraud", null);

        userRepository.GetUserById("actor-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns(target);
        userRepository.UpdateUser(Arg.Any<UserEntity>()).Returns(callInfo => callInfo.Arg<UserEntity>());

        var useCase = new RestrictUserUseCase(userRepository, restrictionRepository);
        var result = await useCase.ExecuteAsync(command);

        Assert.Equal(UserStatus.Banned, result.Status);

        await userRepository.Received(1).UpdateUser(
            Arg.Is<UserEntity>(u => u.Id == "target-id" && u.Status == UserStatus.Banned));
    }

    [Fact]
    public async Task ShouldRestrictUser_WhenActorIsModerator()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var restrictionRepository = Substitute.For<IUserRestrictionRepository>();

        var actor = MakeUser("moderator-id", "Moderator");
        var target = MakeUser("target-id");
        var command = new RestrictUserCommand("target-id", "moderator-id", RestrictionType.Restricted, "Spam", null);

        userRepository.GetUserById("moderator-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns(target);
        userRepository.UpdateUser(Arg.Any<UserEntity>()).Returns(callInfo => callInfo.Arg<UserEntity>());

        var useCase = new RestrictUserUseCase(userRepository, restrictionRepository);
        var result = await useCase.ExecuteAsync(command);

        Assert.Equal(UserStatus.Restricted, result.Status);
        Assert.Equal("Spam", result.Reason);
    }

    [Fact]
    public async Task ShouldThrow_WhenActorTriesToRestrictThemselves()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var restrictionRepository = Substitute.For<IUserRestrictionRepository>();
        var command = new RestrictUserCommand("same-id", "same-id", RestrictionType.Restricted, "reason", null);

        var useCase = new RestrictUserUseCase(userRepository, restrictionRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(command));

        await userRepository.DidNotReceive().GetUserById(Arg.Any<string>());
    }

    [Fact]
    public async Task ShouldThrow_WhenActorHasNoPrivilegedRole()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var restrictionRepository = Substitute.For<IUserRestrictionRepository>();

        var actor = MakeUser("actor-id", "User");
        var command = new RestrictUserCommand("target-id", "actor-id", RestrictionType.Restricted, "reason", null);

        userRepository.GetUserById("actor-id").Returns(actor);

        var useCase = new RestrictUserUseCase(userRepository, restrictionRepository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(command));

        await restrictionRepository.DidNotReceive().AddAsync(Arg.Any<UserRestrictionEntity>());
    }

    [Fact]
    public async Task ShouldThrow_WhenActorHasNoRolesAtAll()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var restrictionRepository = Substitute.For<IUserRestrictionRepository>();

        var actor = MakeUser("actor-id");
        var command = new RestrictUserCommand("target-id", "actor-id", RestrictionType.Restricted, "reason", null);

        userRepository.GetUserById("actor-id").Returns(actor);

        var useCase = new RestrictUserUseCase(userRepository, restrictionRepository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ShouldThrow_WhenActorNotFound()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var restrictionRepository = Substitute.For<IUserRestrictionRepository>();
        var command = new RestrictUserCommand("target-id", "actor-id", RestrictionType.Restricted, "reason", null);

        userRepository.GetUserById("actor-id").Returns((UserEntity?)null);

        var useCase = new RestrictUserUseCase(userRepository, restrictionRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => useCase.ExecuteAsync(command));
    }

    [Fact]
    public async Task ShouldThrow_WhenTargetNotFound()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var restrictionRepository = Substitute.For<IUserRestrictionRepository>();

        var actor = MakeUser("actor-id", "Admin");
        var command = new RestrictUserCommand("target-id", "actor-id", RestrictionType.Restricted, "reason", null);

        userRepository.GetUserById("actor-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns((UserEntity?)null);

        var useCase = new RestrictUserUseCase(userRepository, restrictionRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => useCase.ExecuteAsync(command));

        await restrictionRepository.DidNotReceive().AddAsync(Arg.Any<UserRestrictionEntity>());
    }

    [Fact]
    public async Task ShouldThrow_WhenTargetAlreadyHasRestriction()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var restrictionRepository = Substitute.For<IUserRestrictionRepository>();

        var actor = MakeUser("actor-id", "Admin");
        var target = MakeUser("target-id");
        target.Status = UserStatus.Restricted;
        var command = new RestrictUserCommand("target-id", "actor-id", RestrictionType.Restricted, "reason", null);

        userRepository.GetUserById("actor-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns(target);

        var useCase = new RestrictUserUseCase(userRepository, restrictionRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(command));

        await userRepository.DidNotReceive().UpdateUser(Arg.Any<UserEntity>());
        await restrictionRepository.DidNotReceive().AddAsync(Arg.Any<UserRestrictionEntity>());
    }

    [Fact]
    public async Task ShouldSetExpiresAt_WhenProvided()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var restrictionRepository = Substitute.For<IUserRestrictionRepository>();

        var actor = MakeUser("actor-id", "Admin");
        var target = MakeUser("target-id");
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var command = new RestrictUserCommand("target-id", "actor-id", RestrictionType.Restricted, "Temporary", expiresAt);

        userRepository.GetUserById("actor-id").Returns(actor);
        userRepository.GetUserById("target-id").Returns(target);
        userRepository.UpdateUser(Arg.Any<UserEntity>()).Returns(callInfo => callInfo.Arg<UserEntity>());

        var useCase = new RestrictUserUseCase(userRepository, restrictionRepository);
        var result = await useCase.ExecuteAsync(command);

        Assert.Equal(expiresAt, result.ExpiresAt);

        await restrictionRepository.Received(1).AddAsync(
            Arg.Is<UserRestrictionEntity>(r => r.ExpiresAt == expiresAt));
    }
}
