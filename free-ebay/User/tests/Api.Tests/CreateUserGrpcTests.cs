using Api.GrpcServices;
using Application.UseCases.CreateUser;
using Domain.Entities.User;
using Grpc.Core;
using NSubstitute;
using Protos.User;
using CreateUserResponse = Application.UseCases.CreateUser.CreateUserResponse;

namespace Api.Tests;

public class CreateUser
{
    [Fact]
    public async Task ShouldReturnSuccessResponse()
    {
        // Arrange
        var useCase = Substitute.For<ICreateUserUseCase>();

        var useCaseResponse = new CreateUserResponse(
            "userId",
            "test@example.com",
            "John Doe",
            UserStatus.Active
        );

        useCase.ExecuteAsync(Arg.Any<CreateUserCommand>()).Returns(useCaseResponse);

        var service = new CreateUserGrpcService(useCase);

        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "hashed_password",
            FullName = "John Doe",
            Phone = "+1234567890"
        };

        var response = await service.CreateUser(request, Substitute.For<ServerCallContext>());

        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Equal("userId", response.Data.Id);
        Assert.Equal("John Doe", response.Data.FullName);
        Assert.Equal("test@example.com", response.Data.Email);
        Assert.Equal(UserStatusProto.Active, response.Data.Status);

        await useCase.Received(1).ExecuteAsync(
            Arg.Is<CreateUserCommand>(c =>
                c.Email == request.Email &&
                c.Password == request.Password &&
                c.Fullname == request.FullName &&
                c.Phone == request.Phone
            )
        );
    }
}