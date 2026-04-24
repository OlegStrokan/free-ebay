using Api.Tests.TestHelpers;
using Application.UseCases.AssignRole;
using Application.UseCases.GetAllRoles;
using Application.UseCases.GetUserRoles;
using Application.UseCases.RevokeRole;
using Grpc.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Protos.Role;
using AssignRoleResponseApp   = Application.UseCases.AssignRole.AssignRoleResponse;
using RevokeRoleResponseApp   = Application.UseCases.RevokeRole.RevokeRoleResponse;
using GetUserRolesResponseApp = Application.UseCases.GetUserRoles.GetUserRolesResponse;
using GetAllRolesResponseApp  = Application.UseCases.GetAllRoles.GetAllRolesResponse;

namespace Api.Tests;

public class RoleGrpcTests
{
    // -----------------------------------------------------------------------
    // AssignRole
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AssignRole_ShouldReturnSuccess()
    {
        var useCase = Substitute.For<IAssignRoleUseCase>();
        useCase.ExecuteAsync(Arg.Any<AssignRoleCommand>()).Returns(new AssignRoleResponseApp(true));

        var service = UserGrpcServiceTestFactory.Create(assignRoleUseCase: useCase);
        var request = new AssignRoleRequest { UserId = "user-id", RoleName = "Moderator", AssignedBy = "admin-id" };

        var response = await service.AssignRole(request, Substitute.For<ServerCallContext>());

        Assert.True(response.Success);
        await useCase.Received(1).ExecuteAsync(
            Arg.Is<AssignRoleCommand>(c =>
                c.UserId == "user-id" &&
                c.RoleName == "Moderator" &&
                c.AssignedBy == "admin-id"));
    }

    [Theory]
    [InlineData("", "Admin", "admin-id", "user_id")]
    [InlineData("user-id", "", "admin-id", "role_name")]
    [InlineData("user-id", "Admin", "", "assigned_by")]
    public async Task AssignRole_ShouldThrow_InvalidArgument_WhenRequiredFieldMissing(
        string userId, string roleName, string assignedBy, string expectedField)
    {
        var service = UserGrpcServiceTestFactory.Create();
        var request = new AssignRoleRequest { UserId = userId, RoleName = roleName, AssignedBy = assignedBy };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.AssignRole(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Contains(expectedField, ex.Status.Detail);
    }

    [Fact]
    public async Task AssignRole_ShouldThrow_NotFound_WhenUserDoesNotExist()
    {
        var useCase = Substitute.For<IAssignRoleUseCase>();
        useCase.ExecuteAsync(Arg.Any<AssignRoleCommand>()).Throws(new KeyNotFoundException("User not found"));

        var service = UserGrpcServiceTestFactory.Create(assignRoleUseCase: useCase);
        var request = new AssignRoleRequest { UserId = "missing", RoleName = "Admin", AssignedBy = "admin-id" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.AssignRole(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task AssignRole_ShouldThrow_InvalidArgument_WhenRoleDoesNotExist()
    {
        var useCase = Substitute.For<IAssignRoleUseCase>();
        useCase.ExecuteAsync(Arg.Any<AssignRoleCommand>()).Throws(new ArgumentException("Role 'FakeRole' does not exist"));

        var service = UserGrpcServiceTestFactory.Create(assignRoleUseCase: useCase);
        var request = new AssignRoleRequest { UserId = "user-id", RoleName = "FakeRole", AssignedBy = "admin-id" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.AssignRole(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task AssignRole_ShouldThrow_AlreadyExists_WhenRoleAlreadyAssigned()
    {
        var useCase = Substitute.For<IAssignRoleUseCase>();
        useCase.ExecuteAsync(Arg.Any<AssignRoleCommand>()).Throws(new InvalidOperationException("User already has role"));

        var service = UserGrpcServiceTestFactory.Create(assignRoleUseCase: useCase);
        var request = new AssignRoleRequest { UserId = "user-id", RoleName = "Admin", AssignedBy = "admin-id" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.AssignRole(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.AlreadyExists, ex.StatusCode);
    }

    // -----------------------------------------------------------------------
    // RevokeRole
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RevokeRole_ShouldReturnSuccess()
    {
        var useCase = Substitute.For<IRevokeRoleUseCase>();
        useCase.ExecuteAsync(Arg.Any<RevokeRoleCommand>()).Returns(new RevokeRoleResponseApp(true));

        var service = UserGrpcServiceTestFactory.Create(revokeRoleUseCase: useCase);
        var request = new RevokeRoleRequest { UserId = "user-id", RoleName = "Moderator" };

        var response = await service.RevokeRole(request, Substitute.For<ServerCallContext>());

        Assert.True(response.Success);
        await useCase.Received(1).ExecuteAsync(
            Arg.Is<RevokeRoleCommand>(c => c.UserId == "user-id" && c.RoleName == "Moderator"));
    }

    [Theory]
    [InlineData("", "Admin", "user_id")]
    [InlineData("user-id", "", "role_name")]
    public async Task RevokeRole_ShouldThrow_InvalidArgument_WhenRequiredFieldMissing(
        string userId, string roleName, string expectedField)
    {
        var service = UserGrpcServiceTestFactory.Create();
        var request = new RevokeRoleRequest { UserId = userId, RoleName = roleName };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.RevokeRole(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.Contains(expectedField, ex.Status.Detail);
    }

    [Fact]
    public async Task RevokeRole_ShouldThrow_NotFound_WhenUserDoesNotExist()
    {
        var useCase = Substitute.For<IRevokeRoleUseCase>();
        useCase.ExecuteAsync(Arg.Any<RevokeRoleCommand>()).Throws(new KeyNotFoundException("User not found"));

        var service = UserGrpcServiceTestFactory.Create(revokeRoleUseCase: useCase);
        var request = new RevokeRoleRequest { UserId = "missing", RoleName = "Admin" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.RevokeRole(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task RevokeRole_ShouldThrow_FailedPrecondition_WhenUserDoesNotHaveRole()
    {
        var useCase = Substitute.For<IRevokeRoleUseCase>();
        useCase.ExecuteAsync(Arg.Any<RevokeRoleCommand>()).Throws(new InvalidOperationException("User does not have role"));

        var service = UserGrpcServiceTestFactory.Create(revokeRoleUseCase: useCase);
        var request = new RevokeRoleRequest { UserId = "user-id", RoleName = "Admin" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.RevokeRole(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
    }

    // -----------------------------------------------------------------------
    // GetUserRoles
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUserRoles_ShouldReturnRoles()
    {
        var useCase = Substitute.For<IGetUserRolesUseCase>();
        useCase.ExecuteAsync(Arg.Any<GetUserRolesQuery>())
            .Returns(new GetUserRolesResponseApp(["User", "Seller"]));

        var service = UserGrpcServiceTestFactory.Create(getUserRolesUseCase: useCase);
        var request = new GetUserRolesRequest { UserId = "user-id" };

        var response = await service.GetUserRoles(request, Substitute.For<ServerCallContext>());

        Assert.Equal(2, response.Roles.Count);
        Assert.Equal("User", response.Roles[0].Name);
        Assert.Equal("Seller", response.Roles[1].Name);
    }

    [Fact]
    public async Task GetUserRoles_ShouldThrow_InvalidArgument_WhenUserIdMissing()
    {
        var service = UserGrpcServiceTestFactory.Create();
        var request = new GetUserRolesRequest { UserId = "" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.GetUserRoles(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task GetUserRoles_ShouldThrow_NotFound_WhenUserDoesNotExist()
    {
        var useCase = Substitute.For<IGetUserRolesUseCase>();
        useCase.ExecuteAsync(Arg.Any<GetUserRolesQuery>()).Throws(new KeyNotFoundException("User not found"));

        var service = UserGrpcServiceTestFactory.Create(getUserRolesUseCase: useCase);
        var request = new GetUserRolesRequest { UserId = "missing" };

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => service.GetUserRoles(request, Substitute.For<ServerCallContext>()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    // -----------------------------------------------------------------------
    // GetAllRoles
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllRoles_ShouldReturnAllSystemRoles()
    {
        var useCase = Substitute.For<IGetAllRolesUseCase>();
        useCase.ExecuteAsync().Returns(new GetAllRolesResponseApp(["User", "Seller", "Moderator", "Admin", "SuperAdmin"]));

        var service = UserGrpcServiceTestFactory.Create(getAllRolesUseCase: useCase);

        var response = await service.GetAllRoles(new GetAllRolesRequest(), Substitute.For<ServerCallContext>());

        Assert.Equal(5, response.Roles.Count);
        Assert.Equal("User", response.Roles[0].Name);
        Assert.Equal("SuperAdmin", response.Roles[4].Name);
    }
}
