using Api.Tests.TestHelpers;
using Application.UseCases.VerifyUserEmail;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.User;

namespace Api.Tests;

public class VerifyUserEmailGrpcTests
{
    [Fact]
    public async Task ShouldReturnSuccessTrue_WhenUseCaseReturnsTrue()
    {
        var useCase = Substitute.For<IVerifyUserEmailUseCase>();
        useCase.ExecuteAsync("user-id").Returns(true);

        var service = UserGrpcServiceTestFactory.Create(verifyUserEmailUseCase: useCase);

        var response = await service.VerifyUserEmail(
            new VerifyUserEmailRequest { UserId = "user-id" },
            Substitute.For<ServerCallContext>());

        Assert.True(response.Success);
        await useCase.Received(1).ExecuteAsync("user-id");
    }

    [Fact]
    public async Task ShouldReturnSuccessFalse_WhenUseCaseReturnsFalse()
    {
        var useCase = Substitute.For<IVerifyUserEmailUseCase>();
        useCase.ExecuteAsync("missing-id").Returns(false);

        var service = UserGrpcServiceTestFactory.Create(verifyUserEmailUseCase: useCase);

        var response = await service.VerifyUserEmail(
            new VerifyUserEmailRequest { UserId = "missing-id" },
            Substitute.For<ServerCallContext>());

        Assert.False(response.Success);
    }

    [Fact]
    public async Task ShouldThrowRpcException_WhenUseCaseThrowsArgumentException()
    {
        var useCase = Substitute.For<IVerifyUserEmailUseCase>();
        useCase.ExecuteAsync(Arg.Any<string>())
            .Throws(new ArgumentException("User id is required", "userId"));

        var service = UserGrpcServiceTestFactory.Create(verifyUserEmailUseCase: useCase);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.VerifyUserEmail(new VerifyUserEmailRequest(), Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }
}
