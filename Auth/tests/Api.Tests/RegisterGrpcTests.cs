
using Api.GrpcServices;
using Application.UseCases.Register;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.Auth;
using RegisterResponse = Application.UseCases.Register.RegisterResponse;

namespace Api.Tests;

public class RegisterUseCaseTests
{
    [Fact]
    public async Task ShouldReturnSuccessResponse()
    {
        var logger = Substitute.For<ILogger<RegisterGrpcService>>();
        var useCase = Substitute.For<IRegisterUseCase>();

        var useCaseResponse = new RegisterResponse(
            "userId",
            "oleh@email.com",
            "Sam Faggotman",
            "token",
            "Success"
        );

        useCase.ExecuteAsync(Arg.Any<RegisterCommand>()).Returns(useCaseResponse);

        var service = new RegisterGrpcService(logger, useCase);

        var request = new RegisterRequest
        {
            Email = "userId",
            Password = "password",
            FullName = "Sam Faggotman",
            Phone = "+3029320932"
        };

        var response = await service.Register(request, Substitute.For<ServerCallContext>());

        Assert.Equal("userId", response.UserId);
        Assert.Equal("Success", response.Message);


        await useCase.Received(1).ExecuteAsync(Arg.Is<RegisterCommand>(c =>
            c.Email == request.Email &&
            c.Fullname == request.FullName));
    }

    [Fact]
    public async Task ShouldThrowWhenInvalidOperationExceptionOccurs()
    {
        var logger = Substitute.For<ILogger<RegisterGrpcService>>();
        var useCase = Substitute.For<IRegisterUseCase>();

        useCase.ExecuteAsync(Arg.Any<RegisterCommand>()).Throws(new InvalidOperationException("Email already exists"));
        
        var service = new RegisterGrpcService(logger, useCase);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.Register(new RegisterRequest(), Substitute.For<ServerCallContext>()));


        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Contains("Email already exists", exception.Status.Detail);
    }
    
    [Fact]
    public async Task ShouldThrowWhenUnexpectedExceptionOccurs()
    {
        var logger = Substitute.For<ILogger<RegisterGrpcService>>();
        var useCase = Substitute.For<IRegisterUseCase>();

        useCase.ExecuteAsync(Arg.Any<RegisterCommand>()).Throws(new Exception("Unexpected shit"));
        
        var service = new RegisterGrpcService(logger, useCase);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.Register(new RegisterRequest(), Substitute.For<ServerCallContext>()));


        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Contains("Registration failed", exception.Status.Detail);
        
    }
}