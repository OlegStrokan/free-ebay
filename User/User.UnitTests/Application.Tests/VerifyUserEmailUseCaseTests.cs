using Application.UseCases.VerifyUserEmail;
using Domain.Entities.User;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class VerifyUserEmailUseCaseTests
{
    [Fact]
    public async Task ShouldReturnFalse_WhenUserNotFound()
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserById("missing-id").Returns((UserEntity?)null);

        var useCase = new VerifyUserEmailUseCase(userRepository);

        var result = await useCase.ExecuteAsync("missing-id");

        Assert.False(result);
        await userRepository.DidNotReceive().UpdateUser(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task ShouldVerifyEmailAndPersist_WhenUserExistsAndNotVerified()
    {
        var userRepository = Substitute.For<IUserRepository>();

        var user = new UserEntity
        {
            Id = "user-id",
            Email = "test@example.com",
            Password = "hash",
            Fullname = "John Doe",
            Phone = "+1234567890",
            CountryCode = "DE",
            CustomerTier = CustomerTier.Standard,
            Status = UserStatus.Active,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        userRepository.GetUserById(user.Id).Returns(user);
        userRepository.UpdateUser(Arg.Any<UserEntity>()).Returns(user);

        var useCase = new VerifyUserEmailUseCase(userRepository);

        var result = await useCase.ExecuteAsync(user.Id);

        Assert.True(result);
        Assert.True(user.IsEmailVerified);
        await userRepository.Received(1).UpdateUser(Arg.Is<UserEntity>(u => u.Id == user.Id && u.IsEmailVerified));
    }

    [Fact]
    public async Task ShouldReturnTrueWithoutUpdate_WhenAlreadyVerified()
    {
        var userRepository = Substitute.For<IUserRepository>();

        var user = new UserEntity
        {
            Id = "user-id",
            Email = "test@example.com",
            Password = "hash",
            Fullname = "John Doe",
            Phone = "+1234567890",
            CountryCode = "DE",
            CustomerTier = CustomerTier.Standard,
            Status = UserStatus.Active,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        userRepository.GetUserById(user.Id).Returns(user);

        var useCase = new VerifyUserEmailUseCase(userRepository);

        var result = await useCase.ExecuteAsync(user.Id);

        Assert.True(result);
        await userRepository.DidNotReceive().UpdateUser(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task ShouldThrowArgumentException_WhenUserIdIsEmpty()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var useCase = new VerifyUserEmailUseCase(userRepository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync("  "));

        Assert.Equal("userId", exception.ParamName);
        await userRepository.DidNotReceive().GetUserById(Arg.Any<string>());
    }
}
