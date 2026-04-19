using Api.Tests.TestHelpers;
using Application.UseCases.RestrictUser;
using Application.UseCases.LiftRestriction;
using Domain.Entities.User;
using Domain.Entities.UserRestriction;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.User;
using RestrictUserResponse = Application.UseCases.RestrictUser.RestrictUserResponse;
using LiftRestrictionResponseApp = Application.UseCases.LiftRestriction.LiftRestrictionResponse;

namespace Api.Tests;

public class RestrictUserGrpcTests
{
    private static RestrictUserResponse MakeResponse(string targetId = "target-id", UserStatus status = UserStatus.Restricted) =>
        new(
            targetId,
            "target@example.com",
            "Target User",
            "+1234567890",
            "DE",
            Domain.Entities.User.CustomerTier.Standard,
            status,
            DateTime.UtcNow,
            DateTime.UtcNow,
            RestrictedById: "actor-id",
            RestrictionType: RestrictionType.Restricted,
            Reason: "Violated ToS",
            ExpiresAt: null,
            IsEmailVerified: false);

    [Fact]
    public async Task ShouldRestrictUser_WhenRequestIsValid()
    {
        var useCase = Substitute.For<IRestrictUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<RestrictUserCommand>()).Returns(MakeResponse());

        var service = UserGrpcServiceTestFactory.Create(restrictUserUseCase: useCase);
        var request = new RestrictUserRequest
        {
            TargetUserId = "target-id",
            ActorUserId  = "actor-id",
            Type         = RestrictionTypeProto.RestrictionTypeRestricted,
            Reason       = "Violated ToS",
        };

        var response = await service.RestrictUser(request, Substitute.For<ServerCallContext>());

        Assert.NotNull(response.Data);
        Assert.Equal("target-id", response.Data.Id);
        Assert.Equal(UserStatusProto.Restricted, response.Data.Status);

        await useCase.Received(1).ExecuteAsync(
            Arg.Is<RestrictUserCommand>(c =>
                c.TargetUserId == "target-id" &&
                c.ActorUserId  == "actor-id" &&
                c.Reason       == "Violated ToS"));
    }

    [Fact]
    public async Task ShouldBanUser_WhenTypeIsBanned()
    {
        var useCase = Substitute.For<IRestrictUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<RestrictUserCommand>()).Returns(MakeResponse(status: UserStatus.Banned));

        var service = UserGrpcServiceTestFactory.Create(restrictUserUseCase: useCase);
        var request = new RestrictUserRequest
        {
            TargetUserId = "target-id",
            ActorUserId  = "actor-id",
            Type         = RestrictionTypeProto.RestrictionTypeBanned,
            Reason       = "Fraud",
        };

        var response = await service.RestrictUser(request, Substitute.For<ServerCallContext>());

        Assert.Equal(UserStatusProto.Banned, response.Data.Status);
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
        var request = new RestrictUserRequest { TargetUserId = targetId, ActorUserId = actorId, Reason = reason };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.RestrictUser(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Contains(expectedField, ex.Status.Detail);
    }

    [Fact]
    public async Task ShouldThrow_PermissionDenied_WhenActorLacksPrivilege()
    {
        var useCase = Substitute.For<IRestrictUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<RestrictUserCommand>()).Throws(new UnauthorizedAccessException("Only Admins and Moderators can restrict users"));

        var service = UserGrpcServiceTestFactory.Create(restrictUserUseCase: useCase);
        var request = new RestrictUserRequest { TargetUserId = "target-id", ActorUserId = "actor-id", Reason = "test" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.RestrictUser(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.PermissionDenied, ex.StatusCode);
    }

    [Fact]
    public async Task ShouldThrow_NotFound_WhenUserDoesNotExist()
    {
        var useCase = Substitute.For<IRestrictUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<RestrictUserCommand>()).Throws(new KeyNotFoundException("User not found"));

        var service = UserGrpcServiceTestFactory.Create(restrictUserUseCase: useCase);
        var request = new RestrictUserRequest { TargetUserId = "missing", ActorUserId = "actor-id", Reason = "test" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.RestrictUser(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task ShouldThrow_FailedPrecondition_WhenUserAlreadyRestricted()
    {
        var useCase = Substitute.For<IRestrictUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<RestrictUserCommand>()).Throws(new InvalidOperationException("User already has an active restriction"));

        var service = UserGrpcServiceTestFactory.Create(restrictUserUseCase: useCase);
        var request = new RestrictUserRequest { TargetUserId = "target-id", ActorUserId = "actor-id", Reason = "test" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.RestrictUser(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
    }

    [Fact]
    public async Task ShouldThrow_FailedPrecondition_WhenSelfRestrict()
    {
        var useCase = Substitute.For<IRestrictUserUseCase>();
        useCase.ExecuteAsync(Arg.Any<RestrictUserCommand>()).Throws(new InvalidOperationException("Actor cannot restrict themselves"));

        var service = UserGrpcServiceTestFactory.Create(restrictUserUseCase: useCase);
        var request = new RestrictUserRequest { TargetUserId = "same-id", ActorUserId = "same-id", Reason = "test" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.RestrictUser(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
    }

    // LiftRestriction tests

    [Fact]
    public async Task LiftRestriction_ShouldSucceed_WhenUserIsRestricted()
    {
        var useCase = Substitute.For<ILiftRestrictionUseCase>();
        useCase.ExecuteAsync(Arg.Any<LiftRestrictionCommand>()).Returns(new LiftRestrictionResponseApp(Success: true));

        var service = UserGrpcServiceTestFactory.Create(liftRestrictionUseCase: useCase);
        var request = new LiftRestrictionRequest { TargetUserId = "target-id", ActorUserId = "actor-id" };

        var response = await service.LiftRestriction(request, Substitute.For<ServerCallContext>());

        Assert.True(response.Success);
        await useCase.Received(1).ExecuteAsync(
            Arg.Is<LiftRestrictionCommand>(c => c.TargetUserId == "target-id" && c.ActorUserId == "actor-id"));
    }

    [Theory]
    [InlineData("", "actor-id", "target_user_id")]
    [InlineData("target-id", "", "actor_user_id")]
    public async Task LiftRestriction_ShouldThrow_InvalidArgument_WhenFieldMissing(
        string targetId, string actorId, string expectedField)
    {
        var service = UserGrpcServiceTestFactory.Create();
        var request = new LiftRestrictionRequest { TargetUserId = targetId, ActorUserId = actorId };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.LiftRestriction(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Contains(expectedField, ex.Status.Detail);
    }

    [Fact]
    public async Task LiftRestriction_ShouldThrow_FailedPrecondition_WhenUserNotRestricted()
    {
        var useCase = Substitute.For<ILiftRestrictionUseCase>();
        useCase.ExecuteAsync(Arg.Any<LiftRestrictionCommand>()).Throws(new InvalidOperationException("User has no active restriction"));

        var service = UserGrpcServiceTestFactory.Create(liftRestrictionUseCase: useCase);
        var request = new LiftRestrictionRequest { TargetUserId = "target-id", ActorUserId = "actor-id" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.LiftRestriction(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
    }
}
