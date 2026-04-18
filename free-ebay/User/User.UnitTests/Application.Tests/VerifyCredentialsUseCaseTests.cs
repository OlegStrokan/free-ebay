using Application.UseCases.VerifyCredentials;
using Domain.Common.Interfaces;
using Domain.Entities.User;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class VerifyCredentialsUseCaseTests
{
    [Fact]
    public async Task ShouldReturnUser_WhenCredentialsAreValid()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();

        var existingUser = new UserEntity
        {
            Id = "userId",
            Email = "test@example.com",
            Password = "hashed-password",
            Fullname = "John Doe",
            Phone = "+1234567890",
            CountryCode = "DE",
            CustomerTier = CustomerTier.Subscriber,
            Status = UserStatus.Active,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        userRepository.GetUserByEmail("test@example.com").Returns(existingUser);
        passwordHasher.VerifyPassword("Password123", existingUser.Password).Returns(true);

        var useCase = new VerifyCredentialsUseCase(userRepository, passwordHasher);

        var result = await useCase.ExecuteAsync("  TEST@EXAMPLE.COM  ", "Password123");

        Assert.NotNull(result);
        Assert.Equal(existingUser.Id, result!.Id);
        Assert.Equal(existingUser.Email, result.Email);
        Assert.Equal(existingUser.Fullname, result.Fullname);
        Assert.Equal(existingUser.IsEmailVerified, result.IsEmailVerified);
        Assert.NotNull(result.DeliveryInfos);
        Assert.Empty(result.DeliveryInfos);

        await userRepository.Received(1).GetUserByEmail("test@example.com");
        passwordHasher.Received(1).VerifyPassword("Password123", existingUser.Password);
    }

    [Fact]
    public async Task ShouldReturnNull_WhenPasswordIsInvalid()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();

        var existingUser = new UserEntity
        {
            Id = "userId",
            Email = "test@example.com",
            Password = "hashed-password",
            Fullname = "John Doe",
            Phone = "+1234567890",
            CountryCode = "DE",
            CustomerTier = CustomerTier.Standard,
            Status = UserStatus.Active,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        userRepository.GetUserByEmail("test@example.com").Returns(existingUser);
        passwordHasher.VerifyPassword("wrong-password", existingUser.Password).Returns(false);

        var useCase = new VerifyCredentialsUseCase(userRepository, passwordHasher);

        var result = await useCase.ExecuteAsync("test@example.com", "wrong-password");

        Assert.Null(result);
    }

    [Fact]
    public async Task ShouldThrowArgumentException_WhenPasswordIsEmpty()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var useCase = new VerifyCredentialsUseCase(userRepository, passwordHasher);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync("test@example.com", "  "));

        Assert.Equal("password", exception.ParamName);
        await userRepository.DidNotReceive().GetUserByEmail(Arg.Any<string>());
    }
}