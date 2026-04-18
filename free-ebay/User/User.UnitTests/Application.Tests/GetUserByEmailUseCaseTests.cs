using Application.UseCases.GetUserByEmail;
using Domain.Entities.User;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class GetUserByEmailUseCaseTests
{
    [Fact]
    public async Task ShouldReturnUser_WhenUserExists()
    {
        var userRepository = Substitute.For<IUserRepository>();

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

        var useCase = new GetUserByEmailUseCase(userRepository);

        var result = await useCase.ExecuteAsync("  TEST@EXAMPLE.COM  ");

        Assert.NotNull(result);
        Assert.Equal(existingUser.Id, result!.Id);
        Assert.Equal(existingUser.Email, result.Email);
        Assert.Equal(existingUser.Fullname, result.Fullname);
        Assert.Equal(existingUser.IsEmailVerified, result.IsEmailVerified);
        Assert.NotNull(result.DeliveryInfos);
        Assert.Empty(result.DeliveryInfos);

        await userRepository.Received(1).GetUserByEmail("test@example.com");
    }

    [Fact]
    public async Task ShouldMapDeliveryInfos_WhenPresent()
    {
        var userRepository = Substitute.For<IUserRepository>();

        var delivery = new Domain.Entities.DeliveryInfo.DeliveryInfo
        {
            Id = "di-2",
            UserId = "userId",
            Street = "Oak Ave 5",
            City = "Berlin",
            PostalCode = "10115",
            CountryDestination = "DE",
        };

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
            UpdatedAt = DateTime.UtcNow,
            DeliveryInfos = [delivery],
        };

        userRepository.GetUserByEmail("test@example.com").Returns(existingUser);

        var useCase = new GetUserByEmailUseCase(userRepository);
        var result = await useCase.ExecuteAsync("test@example.com");

        Assert.NotNull(result);
        Assert.Single(result!.DeliveryInfos!);
        Assert.Equal("di-2", result.DeliveryInfos![0].Id);
        Assert.Equal("Oak Ave 5", result.DeliveryInfos[0].Street);
        Assert.Equal("Berlin", result.DeliveryInfos[0].City);
    }

    [Fact]
    public async Task ShouldReturnNull_WhenUserNotFound()
    {
        var userRepository = Substitute.For<IUserRepository>();

        userRepository.GetUserByEmail("missing@example.com").Returns((UserEntity?)null);

        var useCase = new GetUserByEmailUseCase(userRepository);

        var result = await useCase.ExecuteAsync("missing@example.com");

        Assert.Null(result);
        await userRepository.Received(1).GetUserByEmail("missing@example.com");
    }

    [Fact]
    public async Task ShouldThrowArgumentException_WhenEmailIsEmpty()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var useCase = new GetUserByEmailUseCase(userRepository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync("  "));

        Assert.Equal("email", exception.ParamName);
        await userRepository.DidNotReceive().GetUserByEmail(Arg.Any<string>());
    }

    [Fact]
    public async Task ShouldThrowArgumentException_WhenEmailFormatIsInvalid()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var useCase = new GetUserByEmailUseCase(userRepository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync("not-an-email"));

        Assert.Equal("email", exception.ParamName);
        await userRepository.DidNotReceive().GetUserByEmail(Arg.Any<string>());
    }
}
