using Application.UseCases.RevokeRole;
using Domain.Entities.Role;
using Domain.Entities.User;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class RevokeRoleUseCaseTests
{
    private static UserEntity MakeUser(string id, params string[] roleNames)
    {
        var userRoles = roleNames.Select(name =>
        {
            var role = new RoleEntity { Id = $"role-{name.ToLower()}", Name = name, Description = name, IsSystem = true };
            return new UserRoleEntity { UserId = id, RoleId = role.Id, AssignedBy = "system", AssignedAt = DateTime.UtcNow, Role = role };
        }).ToList();

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
            UserRoles = userRoles,
        };
    }

    [Fact]
    public async Task ShouldRevokeRole_WhenValid()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();

        var user = MakeUser("user-id", "Moderator");
        var command = new RevokeRoleCommand("user-id", "Moderator");

        userRepository.GetUserById("user-id").Returns(user);

        var useCase = new RevokeRoleUseCase(userRepository, roleRepository);
        var result = await useCase.ExecuteAsync(command);

        Assert.True(result.Success);
        await roleRepository.Received(1).RevokeRoleAsync("user-id", "Moderator");
    }

    [Fact]
    public async Task ShouldThrow_WhenUserNotFound()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();
        var command = new RevokeRoleCommand("missing-id", "Admin");

        userRepository.GetUserById("missing-id").Returns((UserEntity?)null);

        var useCase = new RevokeRoleUseCase(userRepository, roleRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => useCase.ExecuteAsync(command));

        await roleRepository.DidNotReceive().RevokeRoleAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ShouldThrow_WhenUserDoesNotHaveRole()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();

        var user = MakeUser("user-id"); // no roles
        var command = new RevokeRoleCommand("user-id", "Admin");

        userRepository.GetUserById("user-id").Returns(user);

        var useCase = new RevokeRoleUseCase(userRepository, roleRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(command));

        await roleRepository.DidNotReceive().RevokeRoleAsync(Arg.Any<string>(), Arg.Any<string>());
    }
}
