using Api.GrpcServices;
using Application.UseCases.Login;
using Application.UseCases.RefreshToken;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.Auth;
using LoginResponse = Application.UseCases.Login.LoginResponse;
using RefreshTokenResponse = Application.UseCases.RefreshToken.RefreshTokenResponse;

namespace Api.Tests;

public class RefreshTokenTest
{
    [Fact]
    public async Task ShouldReturnSuccessResponse()
    {
        var logger = Substitute.For<ILogger<RefreshTokenGrpcService>>();
        var useCase = Substitute.For<IRefreshTokenUseCase>();

        var useCaseResponse = new RefreshTokenResponse("accessToken", "refreshToken", 123);

        useCase.ExecuteAsync(Arg.Any<RefreshTokenCommand>()).Returns(useCaseResponse);

        var service = new RefreshTokenGrpcService(logger, useCase);

        var request = new RefreshTokenRequest
        {
            RefreshToken = "refreshToken",
        };

        var response = await service.RefreshToken(request, Substitute.For<ServerCallContext>());

        Assert.Equal(useCaseResponse.AccessToken, response.AccessToken);
        Assert.Equal(useCaseResponse.ExpiresIn, response.ExpiresIn);

        await useCase.Received(1).ExecuteAsync(Arg.Is<RefreshTokenCommand>(c =>
            c.RefreshToken == request.RefreshToken)
        );
    }


    [Fact]
    public async Task ShouldThrowWhenUnauthorizedAccessExceptionOccurs()
    {
        var logger = Substitute.For<ILogger<RefreshTokenGrpcService>>();
        var useCase = Substitute.For<IRefreshTokenUseCase>();
        
        useCase.ExecuteAsync(Arg.Any<RefreshTokenCommand>())
            .Throws(new UnauthorizedAccessException("Invalid email or password"));
        
        var service = new RefreshTokenGrpcService(logger, useCase);

        var exception = await Assert.ThrowsAsync<RpcException>(() => 
            service.RefreshToken(new RefreshTokenRequest(), Substitute.For<ServerCallContext>()));
        
        Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
        Assert.Equal("Invalid email or password", exception.Status.Detail);
    }
    
    

    [Fact]
    public async Task ShouldThrowWhenUnexpectedExceptionOccurs()
    {
        var logger = Substitute.For<ILogger<RefreshTokenGrpcService>>();
        var useCase = Substitute.For<IRefreshTokenUseCase>();
        
        useCase.ExecuteAsync(Arg.Any<RefreshTokenCommand>())
            .Throws(new Exception("Unexpected exception"));
        
        var service = new RefreshTokenGrpcService(logger, useCase);
    
        var exception = await Assert.ThrowsAsync<RpcException>(() => 
            service.RefreshToken(new RefreshTokenRequest(), Substitute.For<ServerCallContext>()));
        
        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Equal("Token refresh failed", exception.Status.Detail);
    }
}