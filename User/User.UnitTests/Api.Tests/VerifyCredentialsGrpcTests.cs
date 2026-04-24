using Api.Tests.TestHelpers;
using Application.UseCases.VerifyCredentials;
using Domain.Entities.User;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.User;
using VerifyCredentialsUseCaseResponse = Application.UseCases.VerifyCredentials.VerifyCredentialsResponse;

namespace Api.Tests;

public class VerifyCredentialsGrpcTests
{
    [Fact]
    public async Task ShouldReturnValidResponse_WhenCredentialsMatch()
    {
        var useCase = Substitute.For<IVerifyCredentialsUseCase>();

        var useCaseResponse = new VerifyCredentialsUseCaseResponse(
            "userId",
            "test@example.com",
            "John Doe",
            "+1234567890",
            "DE",
            CustomerTier.Premium,
            UserStatus.Active,
            DateTime.UtcNow,
            DateTime.UtcNow,
            true);

        useCase.ExecuteAsync("test@example.com", "Password123").Returns(useCaseResponse);

        var service = UserGrpcServiceTestFactory.Create(verifyCredentialsUseCase: useCase);

        var response = await service.VerifyCredentials(
            new VerifyCredentialsRequest { Email = "test@example.com", Password = "Password123" },
            Substitute.For<ServerCallContext>());

        Assert.True(response.IsValid);
        Assert.NotNull(response.Data);
        Assert.Equal("userId", response.Data.Id);
        Assert.Equal("test@example.com", response.Data.Email);

        await useCase.Received(1).ExecuteAsync("test@example.com", "Password123");
    }

    [Fact]
    public async Task ShouldReturnInvalidResponse_WhenCredentialsDoNotMatch()
    {
        var useCase = Substitute.For<IVerifyCredentialsUseCase>();
        useCase.ExecuteAsync("missing@example.com", "Password123")
            .Returns(Task.FromResult<VerifyCredentialsUseCaseResponse?>(null));

        var service = UserGrpcServiceTestFactory.Create(verifyCredentialsUseCase: useCase);

        var response = await service.VerifyCredentials(
            new VerifyCredentialsRequest { Email = "missing@example.com", Password = "Password123" },
            Substitute.For<ServerCallContext>());

        Assert.False(response.IsValid);
        Assert.Null(response.Data);
    }

    [Fact]
    public async Task ShouldThrowRpcException_WhenUseCaseThrowsArgumentException()
    {
        var useCase = Substitute.For<IVerifyCredentialsUseCase>();
        useCase.ExecuteAsync(Arg.Any<string>(), Arg.Any<string>())
            .Throws(new ArgumentException("Password is required", "password"));

        var service = UserGrpcServiceTestFactory.Create(verifyCredentialsUseCase: useCase);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.VerifyCredentials(new VerifyCredentialsRequest(), Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }
}