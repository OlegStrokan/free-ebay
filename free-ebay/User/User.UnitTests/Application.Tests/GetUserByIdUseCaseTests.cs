using System;
using System.Threading.Tasks;
using Application.UseCases.GetUserById;
using Domain.Entities.User;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class GetUserByIdUseCaseTests
{
    [Fact]
    public async Task ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();

        var existingUser = new UserEntity
        {
            Id = "userId",
            Email = "test@example.com",
            Password = "password",
            Fullname = "John Doe",
            Phone = "+1234567890",
            CountryCode = "DE",
            CustomerTier = CustomerTier.Subscriber,
            Status = UserStatus.Active,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        userRepository.GetUserById(existingUser.Id).Returns(existingUser);

        var useCase = new GetUserByIdUseCase(userRepository);
        
        var result = await useCase.ExecuteAsync(existingUser.Id);
        
        Assert.NotNull(result);
        Assert.Equal(existingUser.Id, result.Id);
        Assert.Equal(existingUser.Email, result.Email);
        Assert.Equal(existingUser.Fullname, result.Fullname);
        Assert.Equal(existingUser.Phone, result.Phone);
        Assert.Equal(existingUser.CountryCode, result.CountryCode);
        Assert.Equal(existingUser.CustomerTier, result.CustomerTier);
        Assert.Equal(existingUser.Status, result.Status);
        Assert.Equal(existingUser.IsEmailVerified, result.IsEmailVerified);
        Assert.NotNull(result.DeliveryInfos);
        Assert.Empty(result.DeliveryInfos);

        await userRepository.Received(1).GetUserById(existingUser.Id);
    }

    [Fact]
    public async Task ShouldMapDeliveryInfos_WhenPresent()
    {
        var userRepository = Substitute.For<IUserRepository>();

        var delivery = new Domain.Entities.DeliveryInfo.DeliveryInfo
        {
            Id = "di-1",
            UserId = "userId",
            Street = "Main St 1",
            City = "Prague",
            PostalCode = "11000",
            CountryDestination = "CZ",
        };

        var existingUser = new UserEntity
        {
            Id = "userId",
            Email = "test@example.com",
            Password = "password",
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

        userRepository.GetUserById(existingUser.Id).Returns(existingUser);

        var useCase = new GetUserByIdUseCase(userRepository);
        var result = await useCase.ExecuteAsync(existingUser.Id);

        Assert.NotNull(result);
        Assert.Single(result.DeliveryInfos!);
        Assert.Equal("di-1", result.DeliveryInfos![0].Id);
        Assert.Equal("Main St 1", result.DeliveryInfos[0].Street);
        Assert.Equal("Prague", result.DeliveryInfos[0].City);
        Assert.Equal("11000", result.DeliveryInfos[0].PostalCode);
        Assert.Equal("CZ", result.DeliveryInfos[0].CountryDestination);
    }

    [Fact]
    public async Task ShouldReturnNull_WhenUserNotFound()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();

        userRepository.GetUserById(Arg.Any<string>()).Returns((UserEntity?)null);

        var useCase = new GetUserByIdUseCase(userRepository);

        // Act
        var result = await useCase.ExecuteAsync("nonExistingId");

        // Assert
        Assert.Null(result);

        await userRepository.Received(1).GetUserById("nonExistingId");
    }
}