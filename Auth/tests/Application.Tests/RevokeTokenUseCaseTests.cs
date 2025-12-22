using Application.UseCases.RevokeToken;
using Domain.Entities;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class RevokeTokenUseCaseTests
{
    [Fact]
    public async Task ShouldSuccessfullyRevokeToken()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        
        var existingRefreshToken = new RefreshTokenEntity
        {
            Id = "tokenId",
            UserId = "userId",
            Token = "tokenValue",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        refreshTokenRepository.GetByTokenAsync(existingRefreshToken.Token).Returns(existingRefreshToken);


        var command = new RevokeTokenCommand(existingRefreshToken.Token);

        var useCase = new RevokeTokenUseCase(refreshTokenRepository);

        var result = await useCase.ExecuteAsync(command);
        
        Assert.Equal("Refresh token revoked", result.Message);
        Assert.True(result.Success);
        
        await refreshTokenRepository.Received(1).GetByTokenAsync(command.RefreshToken);
        await refreshTokenRepository.Received(1).RevokeTokenAsync(command.RefreshToken, command.RevokedById);

    }
    
    
    [Fact]
    public async Task ShouldReturnFailureWhenRefreshTokenNotFound()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();

        refreshTokenRepository.GetByTokenAsync(Arg.Any<string>()).Returns((RefreshTokenEntity?)null);

        var command = new RevokeTokenCommand(string.Empty);
        
        var useCase = new RevokeTokenUseCase(refreshTokenRepository);
        
        var result = await useCase.ExecuteAsync(command);
        
        Assert.Equal("Refresh token not found", result.Message);
        Assert.False(result.Success);
        
        await refreshTokenRepository.DidNotReceive().RevokeTokenAsync(Arg.Any<string>(), Arg.Any<string>());
        
  
     
    }
    
     
    [Fact]
    public async Task ShouldReturnFailureWhenRefreshTokenNotFound2()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        
        var existingRefreshToken = new RefreshTokenEntity
        {
            Id = "tokenId",
            UserId = "userId",
            Token = "tokenValue",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = true
        };
        
        refreshTokenRepository.GetByTokenAsync(existingRefreshToken.Token).Returns(existingRefreshToken);

        var command = new RevokeTokenCommand(existingRefreshToken.Token);
        
        var useCase = new RevokeTokenUseCase(refreshTokenRepository);
        
        var result = await useCase.ExecuteAsync(command);
        
        Assert.Equal("Refresh token already revoked", result.Message);
        Assert.False(result.Success);

        await refreshTokenRepository.DidNotReceive().RevokeTokenAsync(Arg.Any<string>(), Arg.Any<string>());
    }
    
    
}