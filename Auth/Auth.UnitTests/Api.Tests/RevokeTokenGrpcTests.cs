using Api.Tests.TestHelpers;
using Application.UseCases.RevokeToken;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.Auth;
using RevokeTokenUseCaseResponse = Application.UseCases.RevokeToken.RevokeTokenResponse;

namespace Api.Tests;

public class RevokeTokenGrpcTests
{
    [Fact]
    public async Task ShouldReturnSuccessResponse()
    {
        var useCase = Substitute.For<IRevokeTokenUseCase>();
        useCase.ExecuteAsync(Arg.Any<RevokeTokenCommand>())
            .Returns(new RevokeTokenUseCaseResponse(true, "Refresh token revoked"));

        var service = AuthGrpcServiceTestFactory.Create(revokeTokenUseCase: useCase);

        var response = await service.RevokeToken(
            new RevokeTokenRequest { RefreshToken = "token" },
            Substitute.For<ServerCallContext>());

        Assert.True(response.Success);
        Assert.Equal("Refresh token revoked", response.Message);

        await useCase.Received(1).ExecuteAsync(Arg.Is<RevokeTokenCommand>(c => c.RefreshToken == "token"));
    }

    [Fact]
    public async Task ShouldThrowInternal_OnUnexpectedException()
    {
        var useCase = Substitute.For<IRevokeTokenUseCase>();
        useCase.ExecuteAsync(Arg.Any<RevokeTokenCommand>()).Throws(new Exception("boom"));

        var service = AuthGrpcServiceTestFactory.Create(revokeTokenUseCase: useCase);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.RevokeToken(new RevokeTokenRequest(), Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.Equal("Token revocation failed", ex.Status.Detail);
    }
}
