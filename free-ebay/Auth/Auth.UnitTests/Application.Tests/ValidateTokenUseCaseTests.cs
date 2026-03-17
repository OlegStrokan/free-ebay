using Application.Common.Interfaces;
using Application.UseCases.ValidateToken;
using Domain.Entities;
using NSubstitute;

namespace Application.Tests;

public class ValidateTokenUseCaseTests
{
    [Fact]
    public async Task ShouldSuccessfullyValidateToken()
    {
        var jwtTokenValidator = Substitute.For<IJwtTokenValidator>();

        var tokenValue = "tokenValue";
        
        var tokenValidationResult = new TokenValidationResult
        {
            IsValid = true,
            UserId = "userId",
        };
        
        jwtTokenValidator.ValidateToken(tokenValue).Returns(tokenValidationResult);
        
        var command = new ValidateTokenCommand(tokenValue);
        
        var useCase = new ValidateTokenUseCase(jwtTokenValidator);
        
        var response = await useCase.ExecuteAsync(command);
        
        Assert.Equal(tokenValidationResult.UserId, response.UserId);
        Assert.True(response.IsValid);
        
        jwtTokenValidator.Received(1).ValidateToken(tokenValue);
    }
    
    [Fact]
    public async Task ShouldReturnFailureWhenRefreshTokenIsInvalid()
    {
        var jwtTokenValidator = Substitute.For<IJwtTokenValidator>();
        
        var tokenValue = "tokenValue";
        
        var tokenValidationResult = new TokenValidationResult
        {
            IsValid = false,
            UserId = null,
        };

        jwtTokenValidator.ValidateToken(tokenValue).Returns(tokenValidationResult);
        
        var command = new ValidateTokenCommand(tokenValue);
        
        var useCase = new ValidateTokenUseCase(jwtTokenValidator);

        var response = await useCase.ExecuteAsync(command);
        
        Assert.Null(response.UserId);
        Assert.False(response.IsValid);
    }
}