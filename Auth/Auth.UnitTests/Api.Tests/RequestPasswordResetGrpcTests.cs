using Api.GrpcServices;
using Api.Tests.TestHelpers;
using Application.UseCases.RequestPasswordReset;
using Grpc.Core;
using RequestPasswordResetResponse = Application.UseCases.RequestPasswordReset.RequestPasswordResetResponse;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.Auth;

namespace Api.Tests;

public class RequestPasswordResetGrpcTests
{
    [Fact]
    public async Task ShouldRequestPasswordReset()
    {
        var useCase = Substitute.For<IRequestPasswordResetUseCase>();

        var useCaseResponse = new RequestPasswordResetResponse(true, "Done!", "resetToken");

        useCase.ExecuteAsync(Arg.Any<RequestPasswordResetCommand>()).Returns(useCaseResponse);
        
        var service = AuthGrpcServiceTestFactory.Create(requestPasswordResetUseCase: useCase);

        var request = new RequestPasswordResetRequest
        {
            Email = "oleg@email.com", 
         //   IpAddress =  "127.0.0.1",
        };

        var response = await service.RequestPasswordReset(request, Substitute.For<ServerCallContext>());
        
        Assert.Equal(useCaseResponse.Message, response.Message);
        Assert.Equal(useCaseResponse.Success, response.Success);
            
        await useCase.Received(1).ExecuteAsync(Arg.Is<RequestPasswordResetCommand>(x =>
            x.Email == request.Email &&
            x.IpAddress == request.IpAddress));

    }
    
    [Fact]
    public async Task ShouldThrowWhenUnexpectedExceptionOccurs()
    {
        var useCase = Substitute.For<IRequestPasswordResetUseCase>();
        
        useCase.ExecuteAsync(Arg.Any<RequestPasswordResetCommand>())
            .Throws(new Exception("Password reset request failed"));
        
        var service = AuthGrpcServiceTestFactory.Create(requestPasswordResetUseCase: useCase);

        var exception = await Assert.ThrowsAsync<RpcException>(() => 
            service.RequestPasswordReset(new RequestPasswordResetRequest(), Substitute.For<ServerCallContext>()));
        
        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Equal("Password reset request failed", exception.Status.Detail);
    }

    
}