using Application.Common.Interfaces;
using Application.UseCases.RefreshToken;
using Domain.Common.Interfaces;
using Domain.Entities;
using Domain.Gateways;
using Domain.Repositories;
using NSubstitute;

namespace Application.Tests;

public class RefreshTokenUseCaseUseCaseTests
{
    [Fact]
    public async Task ShouldRefreshTokenSuccessfullyAndRevokeOldToken()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

        var refreshTokenId = "refreshTokenId";
        var oldRefreshTokenValue = "oldRefreshTokenValue";
        var newRefreshTokenValue = "newRefreshTokenValue";
        var newAccessToken = "newAccessToken";

        var existingRefreshToken = new RefreshTokenEntity
        {
            Id = "oldTokenId",
            UserId = "userId",
            Token = oldRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        var user = new UserGatewayDto
        {
            Id = "userId",
            Email = "oleh@gmail.com",
            FullName = "user_fullname",
            PasswordHash = "hashed_password",
            Status = UserStatus.Active,
            Phone = "+01091939"
        };

        refreshTokenRepository.GetByTokenAsync(oldRefreshTokenValue).Returns(existingRefreshToken);
        idGenerator.GenerateId().Returns(refreshTokenId);
        userGateway.GetUserByIdAsync(user.Id).Returns(user);
        jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email).Returns(newAccessToken);
        jwtTokenGenerator.GenerateRefreshToken().Returns(newRefreshTokenValue);


        var command = new RefreshTokenCommand(oldRefreshTokenValue);

        var useCase = new RefreshTokenUseCase(refreshTokenRepository, idGenerator, userGateway, jwtTokenGenerator);

        var result = await useCase.ExecuteAsync(command);

        Assert.Equal(newAccessToken, result.AccessToken);
        Assert.Equal(newRefreshTokenValue, result.RefreshToken);
        Assert.Equal(3600, result.ExpiresIn);


        await refreshTokenRepository.Received(1).CreateAsync(
            Arg.Is<RefreshTokenEntity>(t =>
                t.Id == refreshTokenId &&
                t.UserId == user.Id &&
                t.Token == newRefreshTokenValue &&
                t.IsRevoked == false
            ));


        await refreshTokenRepository.Received(1)
            .RevokeTokenAsync(oldRefreshTokenValue, replacedByToken: newRefreshTokenValue);
    }


    [Fact]
    public async Task ShouldThrowErrorWhenRefreshTokenIsInvalid()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

        var oldRefreshTokenValue = "oldRefreshTokenValue";

        refreshTokenRepository.GetByTokenAsync(Arg.Any<string>()).Returns((RefreshTokenEntity?)null);

        var command = new RefreshTokenCommand(oldRefreshTokenValue);

        var useCase = new RefreshTokenUseCase(refreshTokenRepository, idGenerator, userGateway, jwtTokenGenerator);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(command));

        Assert.Equal("Invalid refresh token", exception.Message);
    }

    [Fact]
    public async Task ShouldThrowErrorWhenRefreshTokenIsRevoked()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

        var oldRefreshTokenValue = "oldRefreshTokenValue";

        var existingRefreshToken = new RefreshTokenEntity
        {
            Id = "oldTokenId",
            UserId = "userId",
            Token = oldRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = true
        };

        refreshTokenRepository.GetByTokenAsync(oldRefreshTokenValue).Returns(existingRefreshToken);

        var command = new RefreshTokenCommand(oldRefreshTokenValue);

        var useCase = new RefreshTokenUseCase(refreshTokenRepository, idGenerator, userGateway, jwtTokenGenerator);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(command));

        Assert.Equal("Refresh token has been revoked", exception.Message);
    }

    [Fact]
    public async Task ShouldThrowErrorWhenRefreshTokenIsExpired()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        
        var oldRefreshTokenValue = "oldRefreshTokenValue";
        
        var existingRefreshToken = new RefreshTokenEntity
        {
            Id = "oldTokenId",
            UserId = "userId",
            Token = oldRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
        
        refreshTokenRepository.GetByTokenAsync(oldRefreshTokenValue).Returns(existingRefreshToken);

        var command = new RefreshTokenCommand(oldRefreshTokenValue);
        
        var useCase = new RefreshTokenUseCase(refreshTokenRepository, idGenerator, userGateway, jwtTokenGenerator);
        
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(command));
        
        Assert.Equal("Refresh token has been expired", exception.Message);
    }

    [Fact]
    public async Task ShouldThrowErrorWhenUserNotFound()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        
        var oldRefreshTokenValue = "oldRefreshTokenValue";
        
        var existingRefreshToken = new RefreshTokenEntity
        {
            Id = "oldTokenId",
            UserId = "userId",
            Token = oldRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        
        refreshTokenRepository.GetByTokenAsync(oldRefreshTokenValue).Returns(existingRefreshToken);
        userGateway.GetUserByIdAsync(Arg.Any<string>()).Returns((UserGatewayDto?)null);

        var command = new RefreshTokenCommand(oldRefreshTokenValue);
        
        var useCase = new RefreshTokenUseCase(refreshTokenRepository, idGenerator, userGateway, jwtTokenGenerator);
        
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(command));
        
        Assert.Equal("User not found", exception.Message);
    }

    [Fact]
    public async Task ShouldThrowErrorWhenUserIsBlocked()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        
        var oldRefreshTokenValue = "oldRefreshTokenValue";
        
        var existingRefreshToken = new RefreshTokenEntity
        {
            Id = "oldTokenId",
            UserId = "userId",
            Token = oldRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
        
        var user = new UserGatewayDto
        {
            Id = "userId",
            Email = "oleh@gmail.com",
            FullName = "user_fullname",
            PasswordHash = "hashed_password",
            Status = UserStatus.Blocked,
            Phone = "+01091939"
        };
        
        refreshTokenRepository.GetByTokenAsync(oldRefreshTokenValue).Returns(existingRefreshToken);
        userGateway.GetUserByIdAsync(user.Id).Returns(user);
        
        var command = new RefreshTokenCommand(oldRefreshTokenValue);
        
        var useCase =  new RefreshTokenUseCase(refreshTokenRepository, idGenerator, userGateway, jwtTokenGenerator);
        
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(command));
        
        Assert.Equal("User account is blocked", exception.Message);
    }
}