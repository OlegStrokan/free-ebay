using Api.Tests.TestHelpers;
using Application.UseCases.UpdateUserPassword;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.User;

namespace Api.Tests;

public class UpdateUserPasswordGrpcTests
{
    [Fact]
    public async Task ShouldReturnSuccessResponse_WhenPasswordUpdated()
    {
        var useCase = Substitute.For<IUpdateUserPasswordUseCase>();
        useCase.ExecuteAsync(Arg.Any<UpdateUserPasswordCommand>())
            .Returns(new UpdateUserPasswordResult(true, "Password updated successfully"));

        var service = UserGrpcServiceTestFactory.Create(updateUserPasswordUseCase: useCase);

        var response = await service.UpdateUserPassword(
            new UpdateUserPasswordRequest
            {
                UserId = "user-id",
                NewPasswordHash = "new-password-hash"
            },
            Substitute.For<ServerCallContext>());

        Assert.True(response.Success);
        Assert.Equal("Password updated successfully", response.Message);

        await useCase.Received(1).ExecuteAsync(
            Arg.Is<UpdateUserPasswordCommand>(c =>
                c.UserId == "user-id" && c.NewPasswordHash == "new-password-hash"));
    }

    [Fact]
    public async Task ShouldReturnFailureResponse_WhenUserNotFound()
    {
        var useCase = Substitute.For<IUpdateUserPasswordUseCase>();
        useCase.ExecuteAsync(Arg.Any<UpdateUserPasswordCommand>())
            .Returns(new UpdateUserPasswordResult(false, "User with ID missing-id not found"));

        var service = UserGrpcServiceTestFactory.Create(updateUserPasswordUseCase: useCase);

        var response = await service.UpdateUserPassword(
            new UpdateUserPasswordRequest
            {
                UserId = "missing-id",
                NewPasswordHash = "new-password-hash"
            },
            Substitute.For<ServerCallContext>());

        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    [Fact]
    public async Task ShouldThrowRpcException_WhenUseCaseThrowsArgumentException()
    {
        var useCase = Substitute.For<IUpdateUserPasswordUseCase>();
        useCase.ExecuteAsync(Arg.Any<UpdateUserPasswordCommand>())
            .Throws(new ArgumentException("New password hash is required", "NewPasswordHash"));

        var service = UserGrpcServiceTestFactory.Create(updateUserPasswordUseCase: useCase);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.UpdateUserPassword(new UpdateUserPasswordRequest(), Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }
}
