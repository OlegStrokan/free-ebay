using Application.UseCases.CreateUser;
using Domain.Common.Interfaces;
using Domain.Entities.User;
using Domain.Repositories;
using NSubstitute;
using Xunit;

namespace Application.Tests;

public class CreateUserUseCaseTests
{
    [Fact]
    public async Task ShouldCreateUserAndReturnResponse()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();

        const string hashedPassword = "$2a$12$hashed";

        var command = new CreateUserCommand(
            "TestUser@Email.com",
            "password123",
            " Oleh Strokan ",
            "+420123456");

        passwordHasher.HashPassword(command.Password).Returns(hashedPassword);

        var now = DateTime.UtcNow;
        var normalizedEmail = "testuser@email.com";
        UserEntity? createdUser = null;

        var savedUser = new UserEntity
        {
            Id = "generated-by-usecase",
            Email = normalizedEmail,
            Password = hashedPassword,
            Fullname = "Oleh Strokan",
            Phone = command.Phone,
            CountryCode = "DE",
            CustomerTier = CustomerTier.Standard,
            Status = UserStatus.Active,
            IsEmailVerified = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        userRepository.ExistsByEmail(normalizedEmail).Returns(false);
        userRepository
            .CreateUser(Arg.Do<UserEntity>(u => createdUser = u))
            .Returns(callInfo => callInfo.Arg<UserEntity>());

        var useCase = new CreateUserUseCase(userRepository, passwordHasher);

        var result = await useCase.ExecuteAsync(command);

        Assert.True(Guid.TryParse(result.Id, out _));
        Assert.Equal(savedUser.Email, result.Email);
        Assert.Equal(savedUser.Fullname, result.Fullname);
        Assert.Equal(savedUser.CountryCode, result.CountryCode);
        Assert.Equal(savedUser.CustomerTier, result.CustomerTier);
        Assert.Equal(savedUser.Status, result.Status);
        Assert.Equal(savedUser.IsEmailVerified, result.IsEmailVerified);
        Assert.NotNull(createdUser);
        Assert.True(Guid.TryParse(createdUser!.Id, out var _));
        Assert.Equal(normalizedEmail, createdUser.Email);
        Assert.Equal(hashedPassword, createdUser.Password);
        Assert.Equal("Oleh Strokan", createdUser.Fullname);
        Assert.Equal(command.Phone, createdUser.Phone);
        Assert.Equal("DE", createdUser.CountryCode);
        Assert.Equal(CustomerTier.Standard, createdUser.CustomerTier);
        Assert.False(createdUser.IsEmailVerified);

        await userRepository.Received(1).ExistsByEmail(normalizedEmail);
        passwordHasher.Received(1).HashPassword(command.Password);
        await userRepository.Received(1).CreateUser(Arg.Any<UserEntity>());
    }
}