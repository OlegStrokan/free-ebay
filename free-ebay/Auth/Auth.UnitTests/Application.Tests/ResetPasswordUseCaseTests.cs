using Application.Common.Interfaces;
using Application.UseCases.ResetPassword;
using Domain.Entities;
using Domain.Gateways;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class ResetPasswordUseCaseTests
{
    [Fact]
    public async Task ShouldResetPasswordSuccessfully()
    {
        var passwordResetTokenRepository = Substitute.For<IPasswordResetTokenRepository>();
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var userGateway = Substitute.For<IUserGateway>();
        var passwordHasher = Substitute.For<IPasswordHasher>();

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

        passwordResetTokenRepository.GetByTokenAsync(resetToken.Token).Returns(resetToken);
        passwordHasher.HashPassword("password").Returns("hashedPassword");
        userGateway.UpdateUserPasswordAsync(resetToken.UserId, "hashedPassword").Returns(true);

        var command = new ResetPasswordCommand(resetToken.Token, "password");
        
        var useCase = new ResetPasswordUseCase(passwordResetTokenRepository, refreshTokenRepository, userGateway, passwordHasher);

        var result = await useCase.ExecuteAsync(command);
        
        Assert.Equal("Password reset successfully", result.Message);
        Assert.True(result.Success);
        
        passwordHasher.Received(1).HashPassword("password");
        await passwordResetTokenRepository.Received(1).GetByTokenAsync(resetToken.Token);
        await passwordResetTokenRepository.Received(1).MarkAsUsedAsync(resetToken.Token);
        await userGateway.Received(1).UpdateUserPasswordAsync(resetToken.UserId, "hashedPassword");
        await refreshTokenRepository.Received(1).RevokeAllUserTokensAsync(resetToken.UserId);
    }

    [Fact]
    public async Task ShouldReturnFailureWhenTokenNotFound()
    {
        var passwordResetTokenRepository = Substitute.For<IPasswordResetTokenRepository>();
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var userGateway = Substitute.For<IUserGateway>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        
        passwordResetTokenRepository.GetByTokenAsync("token").Returns((PasswordResetTokenEntity?)null);
        
        var command = new ResetPasswordCommand("token", "password");
        
        var useCase = new ResetPasswordUseCase(passwordResetTokenRepository, refreshTokenRepository, userGateway, passwordHasher);
        
        var result = await useCase.ExecuteAsync(command);
        
        Assert.False(result.Success);
        Assert.Equal("Invalid reset token", result.Message);
        
        await passwordResetTokenRepository.Received(1).GetByTokenAsync(command.Token);
    }
    
    [Fact]
    public async Task ShouldReturnFailureWhenTokenHasAlreadyBeenUsed()
    {
        var passwordResetTokenRepository = Substitute.For<IPasswordResetTokenRepository>();
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var userGateway = Substitute.For<IUserGateway>();
        var passwordHasher = Substitute.For<IPasswordHasher>();

        var resetToken = new PasswordResetTokenEntity
        {
            Id = "tokenId",
            UserId = "userId",
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = true,
            // IpAddress = "127.0.0.1",
        };
        
        passwordResetTokenRepository.GetByTokenAsync(resetToken.Token).Returns(resetToken);
        
        
        var command = new ResetPasswordCommand(resetToken.Token, "password");
        
        var useCase = new ResetPasswordUseCase(passwordResetTokenRepository, refreshTokenRepository, userGateway, passwordHasher);
        
        var result = await useCase.ExecuteAsync(command);
        
        Assert.False(result.Success);
        Assert.Equal("Token has already been used", result.Message);
        
        await passwordResetTokenRepository.Received(1).GetByTokenAsync(command.Token);
    }
    
    [Fact]
    public async Task ShouldReturnFailureWhenToken()
    {
        var passwordResetTokenRepository = Substitute.For<IPasswordResetTokenRepository>();
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var userGateway = Substitute.For<IUserGateway>();
        var passwordHasher = Substitute.For<IPasswordHasher>();

        var resetToken = new PasswordResetTokenEntity
        {
            Id = "tokenId",
            UserId = "userId",
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            // IpAddress = "127.0.0.1",
        };
        
        passwordResetTokenRepository.GetByTokenAsync(resetToken.Token).Returns(resetToken);
        
        
        var command = new ResetPasswordCommand(resetToken.Token, "password");
        
        var useCase = new ResetPasswordUseCase(passwordResetTokenRepository, refreshTokenRepository, userGateway, passwordHasher);
        
        var result = await useCase.ExecuteAsync(command);
        
        Assert.False(result.Success);
        Assert.Equal("Token has expired", result.Message);

        await passwordResetTokenRepository.Received(1).GetByTokenAsync(command.Token);
    }
    
    [Fact]
    public async Task ShouldReturnFailureWhenUserServiceFails()
    {
        var passwordResetTokenRepository = Substitute.For<IPasswordResetTokenRepository>();
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var userGateway = Substitute.For<IUserGateway>();
        var passwordHasher = Substitute.For<IPasswordHasher>();

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
        
        passwordResetTokenRepository.GetByTokenAsync(resetToken.Token).Returns(resetToken);
        passwordHasher.HashPassword("password").Returns("hashedPassword");
        userGateway.UpdateUserPasswordAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        
        var command = new ResetPasswordCommand(resetToken.Token, "password");
        
        var useCase = new ResetPasswordUseCase(passwordResetTokenRepository, refreshTokenRepository, userGateway, passwordHasher);
        
        var result = await useCase.ExecuteAsync(command);
        
        Assert.False(result.Success);
        Assert.Equal("Failed to update password in user service", result.Message);
        
        passwordHasher.Received(1).HashPassword("password");
        await passwordResetTokenRepository.Received(1).GetByTokenAsync(resetToken.Token);
        await userGateway.Received(1).UpdateUserPasswordAsync(resetToken.UserId, "hashedPassword");
        
    }
    
}