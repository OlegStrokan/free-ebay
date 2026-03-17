
using Application.UseCases.RequestPasswordReset;
using Domain.Common.Interfaces;
using Domain.Entities;
using Domain.Gateways;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class RequestPasswordResetUseCaseTests
{
    [Fact]
    public async Task ShouldCreatePasswordResetTokenForExistingUser()
    {
        var passwordResetTokenRepository = Substitute.For<IPasswordResetTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        
        var user = new UserGatewayDto
        {
            Id = "userId",
            Email = "example@email.com",
            FullName = "Abdula",
            Phone = "+4202382938"
        };
        
        var resetToken = new PasswordResetTokenEntity
        {
            Id = "tokenId",
            UserId = "userId",
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            // IpAddress = "127.0.0.1",
        };

        userGateway.GetUserByEmailAsync(user.Email).Returns(user);
        passwordResetTokenRepository.CreateAsync(resetToken).Returns(resetToken);
        idGenerator.GenerateId().Returns(resetToken.Id);

        var command = new RequestPasswordResetCommand(user.Email);
        
        var useCase = new RequestPasswordResetUseCase(passwordResetTokenRepository, idGenerator, userGateway);
        var result = await useCase.ExecuteAsync(command);
        
        Assert.NotNull(result.ResetToken);
        Assert.Equal("Password reset link has been sent to your email",  result.Message);
        Assert.True(result.Success);

        await userGateway.Received().GetUserByEmailAsync(user.Email);
        await passwordResetTokenRepository.Received(1).DeleteByUserIdAsync(user.Id);
        await passwordResetTokenRepository.Received(1).CreateAsync(
            Arg.Is<PasswordResetTokenEntity>(t =>
                t.Id == resetToken.Id &&
                t.UserId == user.Id &&
                !string.IsNullOrEmpty(t.Token) &&
                t.IsUsed == false));
    }

    [Fact]
    public async Task ShouldReturnSuccessEvenWhenUserNotFound()
    {
        var passwordResetTokenRepository = Substitute.For<IPasswordResetTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();

        userGateway.GetUserByEmailAsync(Arg.Any<string>()).Returns((UserGatewayDto?)null);
        
        var command = new RequestPasswordResetCommand(string.Empty);

        var useCase = new RequestPasswordResetUseCase(
            passwordResetTokenRepository, idGenerator, userGateway);

        var result = await useCase.ExecuteAsync(command);
        
        Assert.True(result.Success);
        Assert.Equal("If the email exists, a password reset link has been sent", result.Message);
        Assert.Null(result.ResetToken);
        
        await passwordResetTokenRepository.DidNotReceive()
            .CreateAsync(Arg.Any<PasswordResetTokenEntity>());
        
        await passwordResetTokenRepository.DidNotReceive()
            .DeleteByUserIdAsync(Arg.Any<string>());
        
    }
    
}
