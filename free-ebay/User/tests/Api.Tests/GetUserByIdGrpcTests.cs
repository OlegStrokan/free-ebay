using Api.GrpcServices;
using Application.UseCases.GetUserById;
using Domain.Entities.User;
using Grpc.Core;
using NSubstitute;
using Protos.User;
using GetUserByIdResponse = Application.UseCases.GetUserById.GetUserByIdResponse;

namespace Api.Tests;

public class GetUserByIdGrpcTests
{
    [Fact]
    public async Task ShouldReturnUser_WhenUserExists()
    {
        var useCase = Substitute.For<IGetUserByIdUseCase>();

        var useCaseResponse = new GetUserByIdResponse(
            "userId",
            "test@example.com",
            "John Doe",
            "+1234567890",
            UserStatus.Active
        );

        useCase.ExecuteAsync("userId").Returns(useCaseResponse);

        var service = new GetUserByIdGrpcService(useCase);

        var request = new GetUserByidRequest { Id = "userId" };
        
        var response = await service.GetUserById(request, Substitute.For<ServerCallContext>());

        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Equal("userId", response.Data.Id);
        Assert.Equal("test@example.com", response.Data.Email);
        Assert.Equal("John Doe", response.Data.FullName);
        Assert.Equal("+1234567890", response.Data.Phone);
        Assert.Equal(UserStatusProto.Active, response.Data.Status);

        await useCase.Received(1).ExecuteAsync("userId");
    }

    [Fact]
    public async Task ShouldThrowRpcException_WhenUserNotFound()
    {
        var useCase = Substitute.For<IGetUserByIdUseCase>();

        useCase.ExecuteAsync(Arg.Any<string>()).Returns((GetUserByIdResponse?)null);

        var service = new GetUserByIdGrpcService(useCase);

        var request = new GetUserByidRequest { Id = "nonExistingUser" };

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => service.GetUserById(request, Substitute.For<ServerCallContext>())
        );

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
        Assert.Contains("nonExistingUser", exception.Status.Detail);
    }
}