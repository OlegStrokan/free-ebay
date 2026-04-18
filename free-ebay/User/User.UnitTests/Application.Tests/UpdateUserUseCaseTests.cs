using Application.UseCases.UpdateUser;
using Domain.Entities.User;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class UpdateUserUseCaseTests
{
    [Fact]
    public async Task ShouldUpdateUserSuccessfully()
    {
        var userRepository = Substitute.For<IUserRepository>();
      
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
            UpdatedAt = DateTime.UtcNow
        };
        
        var updatedUser = new UserEntity
        {
            Id = "userId",
            Email = "testNew@example.com",
            Password = "password",
            Fullname = "John Updated",
            Phone = "+2334567890",
            CountryCode = "US",
            CustomerTier = CustomerTier.Premium,
            Status = UserStatus.Active,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var command = new UpdateUserCommand(
            "userId",
            "testNew@example.com",
            "John Updated",
            "+2334567890",
            "us",
            CustomerTier.Premium);
        
        userRepository.GetUserById(existingUser.Id).Returns(existingUser);
        userRepository.GetUserByEmail("testnew@example.com").Returns((UserEntity?)null);
        userRepository.UpdateUser(Arg.Any<UserEntity>()).Returns(updatedUser);
        
        var useCase = new UpdateUserUseCase(userRepository);

        var result = await useCase.ExecuteAsync(command);
        
        Assert.Equal(updatedUser.Id, result.Id);
        Assert.Equal(updatedUser.Email, result.Email);
        Assert.Equal(updatedUser.Fullname, result.Fullname);
        Assert.Equal(updatedUser.Phone, result.Phone);
        Assert.Equal(updatedUser.CountryCode, result.CountryCode);
        Assert.Equal(updatedUser.CustomerTier, result.CustomerTier);
        Assert.Equal(updatedUser.IsEmailVerified, result.IsEmailVerified);
        Assert.NotNull(result.DeliveryInfos);
        Assert.Empty(result.DeliveryInfos);

        await userRepository.Received(1).GetUserById("userId");
        await userRepository.Received(1).GetUserByEmail("testnew@example.com");
        await userRepository.Received(1).UpdateUser(
            Arg.Is<UserEntity>(x =>
                x.Id == updatedUser.Id &&
            x.Email == "testnew@example.com" &&
                x.Fullname == updatedUser.Fullname &&
            x.Phone == updatedUser.Phone &&
            x.CountryCode == "US" &&
            x.CustomerTier == CustomerTier.Premium
            ));
    }
    
    [Fact]
    public async Task ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();

        userRepository.GetUserById(Arg.Any<string>()).Returns((UserEntity?)null);

        var command = new UpdateUserCommand(
            "nonExistingUser",
            "email@example.com",
            "Name",
            "+12345293206",
            "DE"
        );

        var useCase = new UpdateUserUseCase(userRepository);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => useCase.ExecuteAsync(command)
        );

        Assert.Contains(command.Id, exception.Message);
        Assert.Contains("not found", exception.Message);
    }
}