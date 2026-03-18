using Api.GrpcServices;
using Api.Tests.TestHelpers;
using Application.UseCases.UpdateUser;
using Domain.Entities.User;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.User;
using UpdateUserResponse = Application.UseCases.UpdateUser.UpdateUserResponse;

namespace Api.Tests;

public class UpdateUserGrpcTests
{
    [Fact]
    public async Task ShouldReturnSuccessResponse()
    {
        var useCase = Substitute.For<IUpdateUserUseCase>();

        var useCaseResponse = new UpdateUserResponse(
            "userId",
            "updated@example.com",
            "Updated Name",
            "+9876543210",
            "US",
            CustomerTier.Premium,
            UserStatus.Active,
            DateTime.UtcNow,
            DateTime.UtcNow,
            true
        );

        useCase.ExecuteAsync(Arg.Any<UpdateUserCommand>()).Returns(useCaseResponse);

        var service = UserGrpcServiceTestFactory.Create(updateUserUseCase: useCase);

        var request = new UpdateUserRequest
        {
            Id = "userId",
            Email = "updated@example.com",
            FullName = "Updated Name",
            Phone = "+9876543210",
            CountryCode = "US",
            CustomerTier = CustomerTierProto.Premium,
        };

        var response = await service.UpdateUser(request, Substitute.For<ServerCallContext>());

        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Equal("userId", response.Data.Id);
        Assert.Equal("updated@example.com", response.Data.Email);
        Assert.Equal("Updated Name", response.Data.FullName);
        Assert.Equal("+9876543210", response.Data.Phone);
        Assert.Equal("US", response.Data.CountryCode);
        Assert.Equal(CustomerTierProto.Premium, response.Data.CustomerTier);
        Assert.Equal(UserStatusProto.Active, response.Data.Status);
        Assert.True(response.Data.IsEmailVerified);

        await useCase.Received(1).ExecuteAsync(
            Arg.Is<UpdateUserCommand>(c =>
                c.Id == request.Id &&
                c.Email == request.Email &&
                c.Fullname == request.FullName &&
                c.Phone == request.Phone &&
                c.CountryCode == request.CountryCode &&
                c.CustomerTier == CustomerTier.Premium
            )
        );
    }

    [Fact]
    public async Task ShouldThrowRpcException_WhenUserNotFound()
    {
        var useCase = Substitute.For<IUpdateUserUseCase>();

        useCase.ExecuteAsync(Arg.Any<UpdateUserCommand>())
            .Throws(new KeyNotFoundException("User with ID userId not found"));

        var service = UserGrpcServiceTestFactory.Create(updateUserUseCase: useCase);

        var request = new UpdateUserRequest
        {
            Id = "userId",
            Email = "email@example.com",
            FullName = "Name",
            Phone = "+123"
        };

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => service.UpdateUser(request, Substitute.For<ServerCallContext>())
        );

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
        Assert.Contains("not found", exception.Status.Detail);
    }
}