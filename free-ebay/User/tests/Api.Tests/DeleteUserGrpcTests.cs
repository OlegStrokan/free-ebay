using Api.GrpcServices;
using Application.UseCases.DeleteUser;
using Grpc.Core;
using NSubstitute;
using Protos.User;

namespace Api.Tests;

public class DeleteUserGrpcTests
{
    [Fact]
    public async Task ShouldReturnEmptyResponse_WhenUserDeleted()
    {
        var useCase = Substitute.For<IDeleteUserUseCase>();

        useCase.ExecuteAsync("userId").Returns(Task.CompletedTask);

        var service = new DeleteUserGrpcService(useCase);

        var request = new DeleteUserRequest { Id = "userId" };
        
        var response = await service.DeleteUser(request, Substitute.For<ServerCallContext>());

        Assert.NotNull(response);

        await useCase.Received(1).ExecuteAsync("userId");
    }
}