using Api.GrpcServices;
using Api.Tests.TestHelpers;
using Application.UseCases.CreateUser;
using Domain.Entities.User;
using Grpc.Core;
using NSubstitute;
using Protos.User;
using CreateUserResponse = Application.UseCases.CreateUser.CreateUserResponse;

namespace Api.Tests;

public class CreateUserGrpcTests
{
    [Fact]
    public async Task ShouldReturnSuccessResponse()
    {
        // Arrange
        var useCase = Substitute.For<ICreateUserUseCase>();
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);

        var useCaseResponse = new CreateUserResponse(
            "userId",
            "test@example.com",
            "John Doe",
            "+1234567890",
            "US",
            CustomerTier.Premium,
            UserStatus.Active,
            createdAt,
            updatedAt,
            true
        );

        useCase.ExecuteAsync(Arg.Any<CreateUserCommand>()).Returns(useCaseResponse);

        var service = UserGrpcServiceTestFactory.Create(createUserUseCase: useCase);

        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "hashed_password",
            FullName = "John Doe",
            Phone = "+1234567890",
            CountryCode = "US",
            CustomerTier = CustomerTierProto.Premium,
        };

        var response = await service.CreateUser(request, Substitute.For<ServerCallContext>());

        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Equal("userId", response.Data.Id);
        Assert.Equal("John Doe", response.Data.FullName);
        Assert.Equal("test@example.com", response.Data.Email);
        Assert.Equal("US", response.Data.CountryCode);
        Assert.Equal(CustomerTierProto.Premium, response.Data.CustomerTier);
        Assert.Equal(UserStatusProto.Active, response.Data.Status);
        Assert.True(response.Data.IsEmailVerified);

        await useCase.Received(1).ExecuteAsync(
            Arg.Is<CreateUserCommand>(c =>
                c.Email == request.Email &&
                c.Password == request.Password &&
                c.Fullname == request.FullName &&
                c.Phone == request.Phone &&
                c.CountryCode == request.CountryCode &&
                c.CustomerTier == CustomerTier.Premium
            )
        );
    }
}