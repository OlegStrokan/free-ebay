using Application.UseCases.GetUserRoles;
using Domain.Entities.Role;
using Domain.Entities.User;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class GetUserRolesUseCaseTests
{
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
    public async Task ShouldReturnRoles_WhenUserExists()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();

        var user = MakeUser("user-id");
        var roles = new List<RoleEntity>
        {
            new() { Id = "role-user",   Name = "User",   Description = "User",   IsSystem = true },
            new() { Id = "role-seller", Name = "Seller", Description = "Seller", IsSystem = true },
        };

        userRepository.GetUserById("user-id").Returns(user);
        roleRepository.GetUserRolesAsync("user-id").Returns(roles);

        var useCase = new GetUserRolesUseCase(userRepository, roleRepository);
        var result = await useCase.ExecuteAsync(new GetUserRolesQuery("user-id"));

        Assert.Equal(2, result.RoleNames.Count);
        Assert.Contains("User", result.RoleNames);
        Assert.Contains("Seller", result.RoleNames);
    }

    [Fact]
    public async Task ShouldReturnEmptyList_WhenUserHasNoRoles()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();

        var user = MakeUser("user-id");

        userRepository.GetUserById("user-id").Returns(user);
        roleRepository.GetUserRolesAsync("user-id").Returns([]);

        var useCase = new GetUserRolesUseCase(userRepository, roleRepository);
        var result = await useCase.ExecuteAsync(new GetUserRolesQuery("user-id"));

        Assert.Empty(result.RoleNames);
    }

    [Fact]
    public async Task ShouldThrow_WhenUserNotFound()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var roleRepository = Substitute.For<IRoleRepository>();

        userRepository.GetUserById("missing").Returns((UserEntity?)null);

        var useCase = new GetUserRolesUseCase(userRepository, roleRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => useCase.ExecuteAsync(new GetUserRolesQuery("missing")));

        await roleRepository.DidNotReceive().GetUserRolesAsync(Arg.Any<string>());
    }
}
