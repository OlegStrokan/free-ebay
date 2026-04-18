using Api.Tests.TestHelpers;
using Application.UseCases.BlockUser;
using Domain.Entities.User;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.User;
using BlockUserResponse = Application.UseCases.BlockUser.BlockUserResponse;

namespace Api.Tests;

public class BlockUserGrpcTests
{
    private static BlockUserResponse MakeResponse(string targetId = "target-id") =>
        new(
            targetId,
            "target@example.com",
            "Target User",
            "+1234567890",
            "DE",
            Domain.Entities.User.CustomerTier.Standard,
            UserStatus.Blocked,
            DateTime.UtcNow,
            DateTime.UtcNow,
            BlockedById: "actor-id",
            Reason: "Violated ToS",
            IsEmailVerified: false);

    [Fact]
    public async Task ShouldBlockUser_WhenRequestIsValid()
    {
        var useCase = Substitute.For<IBlockUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<BlockUserCommand>()).Returns(MakeResponse());

        var service = UserGrpcServiceTestFactory.Create(blockUserUseCase: useCase);
        var request = new BlockUserRequest
        {
            TargetUserId = "target-id",
            ActorUserId  = "actor-id",
            Reason       = "Violated ToS",
        };

        var response = await service.BlockUser(request, Substitute.For<ServerCallContext>());

        Assert.NotNull(response.Data);
        Assert.Equal("target-id", response.Data.Id);
        Assert.Equal(UserStatusProto.Blocked, response.Data.Status);

        await useCase.Received(1).ExecuteAsync(
            Arg.Is<BlockUserCommand>(c =>
                c.TargetUserId == "target-id" &&
                c.ActorUserId  == "actor-id" &&
                c.Reason       == "Violated ToS"));
    }

    [Theory]
    [InlineData("", "actor-id", "reason", "target_user_id")]
    [InlineData("  ", "actor-id", "reason", "target_user_id")]
    [InlineData("target-id", "", "reason", "actor_user_id")]
    [InlineData("target-id", "actor-id", "", "reason")]
    [InlineData("target-id", "actor-id", "  ", "reason")]
    public async Task ShouldThrow_InvalidArgument_WhenRequiredFieldMissing(
        string targetId, string actorId, string reason, string expectedField)
    {
        var service = UserGrpcServiceTestFactory.Create();
        var request = new BlockUserRequest { TargetUserId = targetId, ActorUserId = actorId, Reason = reason };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.BlockUser(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Contains(expectedField, ex.Status.Detail);
    }

    [Fact]
    public async Task ShouldThrow_PermissionDenied_WhenActorLacksPrivilege()
    {
        var useCase = Substitute.For<IBlockUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<BlockUserCommand>()).Throws(new UnauthorizedAccessException("Only Admins and Moderators can block users"));

        var service = UserGrpcServiceTestFactory.Create(blockUserUseCase: useCase);
        var request = new BlockUserRequest { TargetUserId = "target-id", ActorUserId = "actor-id", Reason = "test" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.BlockUser(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.PermissionDenied, ex.StatusCode);
    }

    [Fact]
    public async Task ShouldThrow_NotFound_WhenUserDoesNotExist()
    {
        var useCase = Substitute.For<IBlockUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<BlockUserCommand>()).Throws(new KeyNotFoundException("User not found"));

        var service = UserGrpcServiceTestFactory.Create(blockUserUseCase: useCase);
        var request = new BlockUserRequest { TargetUserId = "missing", ActorUserId = "actor-id", Reason = "test" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.BlockUser(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task ShouldThrow_FailedPrecondition_WhenUserAlreadyBlocked()
    {
        var useCase = Substitute.For<IBlockUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<BlockUserCommand>()).Throws(new InvalidOperationException("User is already blocked"));

        var service = UserGrpcServiceTestFactory.Create(blockUserUseCase: useCase);
        var request = new BlockUserRequest { TargetUserId = "target-id", ActorUserId = "actor-id", Reason = "test" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.BlockUser(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
    }

    [Fact]
    public async Task ShouldThrow_FailedPrecondition_WhenSelfBlock()
    {
        var useCase = Substitute.For<IBlockUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<BlockUserCommand>()).Throws(new InvalidOperationException("User cannot block themselves"));

        var service = UserGrpcServiceTestFactory.Create(blockUserUseCase: useCase);
        var request = new BlockUserRequest { TargetUserId = "same-id", ActorUserId = "same-id", Reason = "test" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.BlockUser(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
    }
}
