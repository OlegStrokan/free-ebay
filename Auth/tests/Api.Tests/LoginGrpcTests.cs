using Api.GrpcServices;
using Application.UseCases.Login;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.Auth;
using LoginResponse = Application.UseCases.Login.LoginResponse;

namespace Api.Tests;

public class LoginGrpcTests
{
    [Fact]
    public async Task ShouldReturnSuccessResponse()
    {
        var logger = Substitute.For<ILogger<LoginGrpcService>>();
        var useCase = Substitute.For<ILoginUseCase>();

        var useCaseResponse = new LoginResponse(
            "accessToken",
            "refreshToken",
            3600,
            "Bearer"
        );
        
        useCase.ExecuteAsync(Arg.Any<LoginCommand>()).Returns(useCaseResponse);
        
        var service = new LoginGrpcService(logger, useCase);

        var request = new LoginRequest
        {
            Email = "oleh@email.com",
            Password = "password"
        };

        var response = await service.Login(request, Substitute.For<ServerCallContext>());
        
        Assert.Equal(useCaseResponse.AccessToken, response.AccessToken);
        Assert.Equal(useCaseResponse.RefreshToken, response.RefreshToken);
        Assert.Equal(useCaseResponse.ExpiresIn, response.ExpiresIn);
        Assert.Equal(useCaseResponse.TokenType, response.TokenType);
        
        await useCase.Received(1).ExecuteAsync(Arg.Is<LoginCommand>(c => 
            c.Email == request.Email &&
            c.Password == request.Password));
    }


    [Fact]
    public async Task ShouldThrowWhenUnauthorizedAccessExceptionOccurs()
    {
        var logger = Substitute.For<ILogger<LoginGrpcService>>();
        var useCase = Substitute.For<ILoginUseCase>();
        
        useCase.ExecuteAsync(Arg.Any<LoginCommand>())
            .Throws(new UnauthorizedAccessException("Invalid email or password"));
        
        var service = new LoginGrpcService(logger, useCase);

        var exception = await Assert.ThrowsAsync<RpcException>(() => 
            service.Login(new LoginRequest(), Substitute.For<ServerCallContext>()));
        
        Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
        Assert.Equal("Invalid email or password", exception.Status.Detail);
    }
    
    

    [Fact]
    public async Task ShouldThrowWhenUnexpectedExceptionOccurs()
    {
        var logger = Substitute.For<ILogger<LoginGrpcService>>();
        var useCase = Substitute.For<ILoginUseCase>();
        
        useCase.ExecuteAsync(Arg.Any<LoginCommand>())
            .Throws(new Exception("Unexpected exception"));
        
        var service = new LoginGrpcService(logger, useCase);
    
        var exception = await Assert.ThrowsAsync<RpcException>(() => 
            service.Login(new LoginRequest(), Substitute.For<ServerCallContext>()));
        
        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Equal("Login failed", exception.Status.Detail);
    }
}