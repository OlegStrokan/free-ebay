using Application.UseCases.VerifyEmail;
using Domain.Entities;
using Domain.Gateways;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class VerifyEmailUseCaseTests
{
    [Fact]
    public async Task ShouldSuccessfullyVerifyEmail()
    {
        var emailVerificationRepository = Substitute.For<IEmailVerificationTokenRepository>();
        var userGateway = Substitute.For<IUserGateway>();

        var emailVerificationToken = new EmailVerificationTokenEntity
        {
            UserId = "userId",
            Id = "tokenId",
            IsUsed = false,
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        emailVerificationRepository.GetByTokenAsync(emailVerificationToken.Token).Returns(emailVerificationToken);
        userGateway.VerifyUserEmailAsync(emailVerificationToken.UserId).Returns(true);
        
        var command = new VerifyEmailCommand(emailVerificationToken.Token);
        
        var useCase = new VerifyEmailUseCase(emailVerificationRepository, userGateway);

        var result = await useCase.ExecuteAsync(command);
        
        Assert.Equal(emailVerificationToken.UserId, result.UserId);
        Assert.Equal("Email verified successfully", result.Message);
        Assert.True(result.Success);
        
        
        await emailVerificationRepository.Received(1).GetByTokenAsync(emailVerificationToken.Token);
        await userGateway.Received(1).VerifyUserEmailAsync(emailVerificationToken.UserId);
        
    }

    [Fact]
    public async Task ShouldReturnFailureWhenVerificationTokenNotFound()
    {
        var emailVerificationRepository = Substitute.For<IEmailVerificationTokenRepository>();
        var userGateway = Substitute.For<IUserGateway>();
        
        emailVerificationRepository.GetByTokenAsync(Arg.Any<string>()).Returns((EmailVerificationTokenEntity?)null);
        
        var command = new VerifyEmailCommand("token");
        
        var useCase = new VerifyEmailUseCase(emailVerificationRepository, userGateway);

        var result = await useCase.ExecuteAsync(command);

        Assert.Equal("Invalid verification token", result.Message);
        Assert.False(result.Success);
        Assert.Null(result.UserId);
        
        await emailVerificationRepository.Received(1).GetByTokenAsync("token");
        await userGateway.DidNotReceive().VerifyUserEmailAsync(Arg.Any<string>());
    }
    
    [Fact]
    public async Task ShouldReturnFailureWhenVerificationTokenAlreadyUsed()
    {
        var emailVerificationRepository = Substitute.For<IEmailVerificationTokenRepository>();
        var userGateway = Substitute.For<IUserGateway>();
        
        var emailVerificationToken = new EmailVerificationTokenEntity
        {
            UserId = "userId",
            Id = "tokenId",
            IsUsed = true,
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        emailVerificationRepository.GetByTokenAsync(emailVerificationToken.Token).Returns(emailVerificationToken);

        var command = new VerifyEmailCommand(emailVerificationToken.Token);
        
        var useCase = new VerifyEmailUseCase(emailVerificationRepository, userGateway);

        var result = await useCase.ExecuteAsync(command);

        Assert.Equal("Token has already been used", result.Message);
        Assert.False(result.Success);
        Assert.Null(result.UserId);
        
        await emailVerificationRepository.Received(1).GetByTokenAsync("token");
        await userGateway.DidNotReceive().VerifyUserEmailAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ShouldReturnFailureWhenVerificationTokenExpired()
    {
        var emailVerificationRepository = Substitute.For<IEmailVerificationTokenRepository>();
        var userGateway = Substitute.For<IUserGateway>();
        
        var emailVerificationToken = new EmailVerificationTokenEntity
        {
            UserId = "userId",
            Id = "tokenId",
            IsUsed = false,
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        };
        emailVerificationRepository.GetByTokenAsync(emailVerificationToken.Token).Returns(emailVerificationToken);

        var command = new VerifyEmailCommand(emailVerificationToken.Token);
        
        var useCase = new VerifyEmailUseCase(emailVerificationRepository, userGateway);

        var result = await useCase.ExecuteAsync(command);

        Assert.Equal("Token has expired", result.Message);
        Assert.False(result.Success);
        Assert.Null(result.UserId);
        
        await emailVerificationRepository.Received(1).GetByTokenAsync("token");
        await userGateway.DidNotReceive().VerifyUserEmailAsync(Arg.Any<string>());
    }
    
    [Fact]
    public async Task ShouldReturnFailureWheVerifyUserFailed()
    {
        var emailVerificationRepository = Substitute.For<IEmailVerificationTokenRepository>();
        var userGateway = Substitute.For<IUserGateway>();
        
        var emailVerificationToken = new EmailVerificationTokenEntity
        {
            UserId = "userId",
            Id = "tokenId",
            IsUsed = false,
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        emailVerificationRepository.GetByTokenAsync(emailVerificationToken.Token).Returns(emailVerificationToken);
        userGateway.VerifyUserEmailAsync(emailVerificationToken.UserId).Returns(false);
        
        var command = new VerifyEmailCommand(emailVerificationToken.Token);
        
        var useCase = new VerifyEmailUseCase(emailVerificationRepository, userGateway);

        var result = await useCase.ExecuteAsync(command);

        Assert.Equal("Failed to verify email in user service", result.Message);
        Assert.False(result.Success);
        Assert.Null(result.UserId);
        
        await emailVerificationRepository.Received(1).GetByTokenAsync("token");
        await emailVerificationRepository.Received(1).MarkAsUsedAsync("token");
        await userGateway.Received().VerifyUserEmailAsync(emailVerificationToken.UserId);

    }
}