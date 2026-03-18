using Api.Tests.TestHelpers;
using Application.UseCases.GetUserByEmail;
using Domain.Entities.User;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.User;
using GetUserByEmailUseCaseResponse = Application.UseCases.GetUserByEmail.GetUserByEmailResponse;

namespace Api.Tests;

public class GetUserByEmailGrpcTests
{
    [Fact]
    public async Task ShouldReturnUserWithPasswordHash_WhenUserExists()
    {
        var useCase = Substitute.For<IGetUserByEmailUseCase>();

        var useCaseResponse = new GetUserByEmailUseCaseResponse(
            "userId",
            "test@example.com",
            "John Doe",
            "+1234567890",
            "DE",
            CustomerTier.Premium,
            UserStatus.Active,
            DateTime.UtcNow,
            DateTime.UtcNow,
            true,
            "$2a$12$hash");

        useCase.ExecuteAsync("test@example.com").Returns(useCaseResponse);

        var service = UserGrpcServiceTestFactory.Create(getUserByEmailUseCase: useCase);

        var response = await service.GetUserByEmail(
            new GetUserByEmailRequest { Email = "test@example.com" },
            Substitute.For<ServerCallContext>());

        Assert.NotNull(response.Data);
        Assert.Equal("userId", response.Data.Id);
        Assert.Equal("test@example.com", response.Data.Email);
        Assert.Equal("$2a$12$hash", response.PasswordHash);
        Assert.True(response.Data.IsEmailVerified);

        await useCase.Received(1).ExecuteAsync("test@example.com");
    }

    [Fact]
    public async Task ShouldReturnEmptyResponse_WhenUserNotFound()
    {
        var useCase = Substitute.For<IGetUserByEmailUseCase>();
        useCase.ExecuteAsync("missing@example.com")
            .Returns(Task.FromResult<GetUserByEmailUseCaseResponse?>(null));

        var service = UserGrpcServiceTestFactory.Create(getUserByEmailUseCase: useCase);

        var response = await service.GetUserByEmail(
            new GetUserByEmailRequest { Email = "missing@example.com" },
            Substitute.For<ServerCallContext>());

        Assert.Null(response.Data);
        Assert.Equal(string.Empty, response.PasswordHash);
    }

    [Fact]
    public async Task ShouldThrowRpcException_WhenUseCaseThrowsArgumentException()
    {
        var useCase = Substitute.For<IGetUserByEmailUseCase>();
        useCase.ExecuteAsync(Arg.Any<string>())
            .Throws(new ArgumentException("Email is required", "email"));

        var service = UserGrpcServiceTestFactory.Create(getUserByEmailUseCase: useCase);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetUserByEmail(new GetUserByEmailRequest(), Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }
}
