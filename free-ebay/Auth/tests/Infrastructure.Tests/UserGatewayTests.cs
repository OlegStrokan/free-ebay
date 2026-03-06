using Grpc.Core;
using Infrastructure.Gateways;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.User;


namespace Infrastructure.Tests;

public class UserGatewayTests
{
    [Fact]
    public async Task ShouldReturnsUserId_WhenUserIsCreatedSuccessfully()
    {
        var logger = Substitute.For<ILogger<UserGateway>>();
        var client = Substitute.For<UserServiceProto.UserServiceProtoClient>();
        var sut = new UserGateway(client, logger);

        var email = "test@example.com";
        var expectedId = "userId";

        var response = new CreateUserResponse { Data = new UserProto { Id = expectedId } };

        client.CreateUserAsync(Arg.Any<CreateUserRequest>())
            .Returns(GrpcTestHelper.CreateAsyncUnaryCall(response));


        var result = await sut.CreateUserAsync(email, "password", "John Hitler", "+42020398298");

        Assert.Equal(expectedId, result);
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationException_WhenResponseDataIsNull()
    {
        var logger = Substitute.For<ILogger<UserGateway>>();
        var client = Substitute.For<UserServiceProto.UserServiceProtoClient>();
        var sut = new UserGateway(client, logger);
        
        client.CreateUserAsync(Arg.Any<CreateUserRequest>())
            .Returns(GrpcTestHelper.CreateAsyncUnaryCall(new CreateUserResponse { Data = null }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateUserAsync("test@test.com", "hashedPassword", "Just Hitler", "+3920239200"));
        
        Assert.Equal("User microservice returned null user", exception.Message);
    }
    
        [Fact]
    public async Task ShouldReturnNullWhenUserNotFoundByEmail()
    {
        var logger = Substitute.For<ILogger<UserGateway>>();
        var client = Substitute.For<UserServiceProto.UserServiceProtoClient>();
        var sut = new UserGateway(client, logger);

        var rpcException = new RpcException(new Status(StatusCode.NotFound, "Not Found"));
        client.GetUserByEmailAsync(Arg.Any<GetUserByEmailRequest>()).Throws(rpcException);
        
        var result = await sut.GetUserByEmailAsync("missing@test.com");

        Assert.Null(result);
        //await client.Received(1).GetUserByEmailAsync(Arg.Is<GetUserByEmailRequest>(r => r.Email == "missing@test.com"));
    }

    [Fact]
    public async Task ShouldMapAndReturnUserDtoWhenUserExistsById()
    {
        var logger = Substitute.For<ILogger<UserGateway>>();
        var client = Substitute.For<UserServiceProto.UserServiceProtoClient>();
        var sut = new UserGateway(client, logger);

        var userId = "userId";
        var response = new GetUserByIdResponse
        {
            Data = new UserProto
            {
                Id = userId,
                Email = "found@test.com",
                FullName = "Found User",
                Status = UserStatusProto.Active
            }
        };

        client.GetUserByIdAsync(Arg.Any<GetUserByIdRequest>())
            .Returns(GrpcTestHelper.CreateAsyncUnaryCall(response));

        var result = await sut.GetUserByIdAsync(userId);
        
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("Found User", result.FullName);
       // await client.Received(1).GetUserByIdAsync(Arg.Is<GetUserByIdRequest>(r => r.Id == userId));
    }

    [Fact]
    public async Task ShouldReturnTrueWhenEmailIsVerifiedSuccessfully()
    {
        var logger = Substitute.For<ILogger<UserGateway>>();
        var client = Substitute.For<UserServiceProto.UserServiceProtoClient>();
        var sut = new UserGateway(client, logger);

        client.VerifyUserEmailAsync(Arg.Any<VerifyUserEmailRequest>())
            .Returns(GrpcTestHelper.CreateAsyncUnaryCall(new VerifyUserEmailResponse { Success = true }));

        var result = await sut.VerifyUserEmailAsync("userId");

        Assert.True(result);
       // await client.Received(1).VerifyUserEmailAsync(Arg.Is<VerifyUserEmailRequest>(r => r.UserId == "user-1"));
    }

    [Fact]
    public async Task ShouldThrowInvalidOperationExceptionWhenPasswordUpdateFails()
    {
        var logger = Substitute.For<ILogger<UserGateway>>();
        var client = Substitute.For<UserServiceProto.UserServiceProtoClient>();
        var sut = new UserGateway(client, logger);

        client.UpdateUserPasswordAsync(Arg.Any<UpdateUserPasswordRequest>())
            .Throws(new RpcException(new Status(StatusCode.Internal, "Database error")));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            sut.UpdateUserPasswordAsync("userId", "newHashedPassword"));

        Assert.Contains("Failed to update password", exception.Message);
    }
}


// helper to satisfy gprc's asyncUnaryCall return type
public static class GrpcTestHelper
{
    public static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}