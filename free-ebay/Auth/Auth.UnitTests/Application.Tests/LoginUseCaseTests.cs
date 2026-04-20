using System;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.UseCases.Login;
using Domain.Common.Interfaces;
using Domain.Entities;
using Domain.Gateways;
using Domain.Repositories;
using NSubstitute;
using Xunit;

namespace Application.Tests;

public class LoginUseCaseTests
{
    [Fact]
    public async Task ShouldLoginSuccessfullyAndReturnTokens()
    {
        // arrange and shit

        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var idGenerator =  Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

        var generatedTokenId = "token_ulid_id";
        var accessToken = "access_token";
        var refreshToken = "refresh_token";

        var user = new UserGatewayDto
        {
            Id = "user_ulid_id",
            Email = "oleh@gmail.com",
            FullName = "user_fullname",
            Status = UserStatus.Active,
            Phone = "+01091939",
            IsEmailVerified = true
        };

        // add "real" mocks
        
        idGenerator.GenerateId().Returns(generatedTokenId);
        userGateway.VerifyCredentialsAsync("oleh@gmail.com", "password123").Returns(user);
        jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Roles).Returns(accessToken);
        jwtTokenGenerator.GenerateRefreshToken().Returns(refreshToken);


        var request = new LoginCommand(user.Email, "password123");

        var useCase = new LoginUseCase(
            refreshTokenRepository, idGenerator, userGateway, jwtTokenGenerator);


        var result = await useCase.ExecuteAsync(request);
        
        Assert.Equal(accessToken, result.AccessToken);
        Assert.Equal(refreshToken, result.RefreshToken);
        Assert.Equal(3600, result.ExpiresIn);
        Assert.Equal("Bearer", result.TokenType);

        
        await refreshTokenRepository.Received(1).CreateAsync(
           Arg.Is<RefreshTokenEntity>( t=>
               t.Id == generatedTokenId &&
               t.IsRevoked == false &&
               t.Token == refreshToken && 
               t.UserId == user.Id
               ));
    }


    [Fact]
    public async Task ShouldThrowExceptionWhenUserNotFound()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        
        // mock only userGateway to throw correct error
        userGateway.VerifyCredentialsAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((UserGatewayDto?)null);

        var command = new LoginCommand("non_existing_email@mail.com", "password1939");
        
        var useCase = new LoginUseCase(
            refreshTokenRepository,
            idGenerator,
            userGateway,
            jwtTokenGenerator);

        var exception = 
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(command));
        

        Assert.Equal("Invalid email or password", exception.Message);

    }

    [Fact]
    public async Task ShouldThrowExceptionWhenUserVerifyPasswordFailed()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

        userGateway.VerifyCredentialsAsync("oleh@gmail.com", "password1939").Returns((UserGatewayDto?)null);

        var command = new LoginCommand("oleh@gmail.com", "password1939");
        var useCase = new LoginUseCase(
            refreshTokenRepository, idGenerator, userGateway, jwtTokenGenerator);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(command));
        
        Assert.Equal("Invalid email or password", exception.Message);
    }

    [Fact]
    public async Task ShouldThrowExceptionWhenUserIsBanned()
    {
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var userGateway = Substitute.For<IUserGateway>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

        
        var user = new UserGatewayDto
        {
            Id = "user_ulid_id",
            Email = "oleh@gmail.com",
            FullName = "user_fullname",
            Status = UserStatus.Banned,
            Phone = "+01091939",
            IsEmailVerified = true
        };

        userGateway.VerifyCredentialsAsync("oleh@gmail.com", "password1939").Returns(user);

        var command = new LoginCommand("oleh@gmail.com", "password1939");
        var useCase = new LoginUseCase(
            refreshTokenRepository, idGenerator, userGateway, jwtTokenGenerator);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.ExecuteAsync(command));

        
        Assert.Equal("Your account has been banned",  exception.Message);
    }
    
}