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
            Status = UserStatus.Active,
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
        Assert.Equal(existingUser.Status, result.Status);

        await userRepository.Received(1).GetUserById(existingUser.Id);
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