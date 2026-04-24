using Api.Tests.TestHelpers;
using Application.UseCases.ResetPassword;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.Auth;
using ResetPasswordUseCaseResponse = Application.UseCases.ResetPassword.ResetPasswordResponse;

namespace Api.Tests;

public class ResetPasswordGrpcTests
{
    [Fact]
    public async Task ShouldReturnResetPasswordResult()
    {
        var useCase = Substitute.For<IResetPasswordUseCase>();
        useCase.ExecuteAsync(Arg.Any<ResetPasswordCommand>())
            .Returns(new ResetPasswordUseCaseResponse(true, "Password reset successfully"));

        var service = AuthGrpcServiceTestFactory.Create(resetPasswordUseCase: useCase);

        var response = await service.ResetPassword(
            new ResetPasswordRequest
            {
                Token = "reset-token",
                NewPassword = "newPassword123"
            },
            Substitute.For<ServerCallContext>());

        Assert.True(response.Success);
        Assert.Equal("Password reset successfully", response.Message);

        await useCase.Received(1).ExecuteAsync(Arg.Is<ResetPasswordCommand>(c =>
            c.Token == "reset-token" && c.NewPassword == "newPassword123"));
    }

    [Fact]
    public async Task ShouldThrowInternal_OnUnexpectedException()
    {
        var useCase = Substitute.For<IResetPasswordUseCase>();
        useCase.ExecuteAsync(Arg.Any<ResetPasswordCommand>()).Throws(new Exception("boom"));

        var service = AuthGrpcServiceTestFactory.Create(resetPasswordUseCase: useCase);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.ResetPassword(new ResetPasswordRequest(), Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.Equal("Password reset request failed", ex.Status.Detail);
    }
}
