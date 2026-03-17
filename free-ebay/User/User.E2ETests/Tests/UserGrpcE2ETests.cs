using FluentAssertions;
using Grpc.Core;
using Protos.User;
using User.E2ETests.Infrastructure;
using Xunit;

namespace User.E2ETests.Tests;

[Collection("E2E")]
public sealed class UserGrpcE2ETests : IClassFixture<E2ETestServer>, IAsyncLifetime
{
    private readonly E2ETestServer _server;
    private UserServiceProto.UserServiceProtoClient _client = null!;

    public UserGrpcE2ETests(E2ETestServer server)
    {
        _server = server;
    }

    public Task InitializeAsync()
    {
        _client = _server.CreateUserClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateUser_ThenGetUserById_ShouldReturnNormalizedData()
    {
        var rawEmail = $"  USER-{Guid.NewGuid():N}@Example.COM  ";

        var created = await _client.CreateUserAsync(new CreateUserRequest
        {
            FullName = "  John Doe  ",
            Email = rawEmail,
            Password = "password123",
            Phone = "  +420123456  ",
            CountryCode = " de ",
            CustomerTier = CustomerTierProto.Premium,
        });

        created.Data.Should().NotBeNull();
        created.Data.Id.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(created.Data.Id, out _).Should().BeTrue();
        created.Data.Email.Should().Be(rawEmail.Trim().ToLowerInvariant());
        created.Data.FullName.Should().Be("John Doe");
        created.Data.Phone.Should().Be("+420123456");
        created.Data.CountryCode.Should().Be("DE");
        created.Data.CustomerTier.Should().Be(CustomerTierProto.Premium);
        created.Data.Status.Should().Be(UserStatusProto.Active);

        var fetched = await _client.GetUserByIdAsync(new GetUserByIdRequest
        {
            Id = created.Data.Id
        });

        fetched.Data.Should().NotBeNull();
        fetched.Data.Id.Should().Be(created.Data.Id);
        fetched.Data.Email.Should().Be(rawEmail.Trim().ToLowerInvariant());
        fetched.Data.CountryCode.Should().Be("DE");
        fetched.Data.CustomerTier.Should().Be(CustomerTierProto.Premium);
        fetched.Data.Status.Should().Be(UserStatusProto.Active);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ShouldReturnAlreadyExists()
    {
        var email = $"dupe-{Guid.NewGuid():N}@example.com";

        await _client.CreateUserAsync(new CreateUserRequest
        {
            FullName = "User One",
            Email = email,
            Password = "password123",
            Phone = "+111111",
            CountryCode = "DE",
            CustomerTier = CustomerTierProto.Standard,
        });

        Func<Task> act = async () =>
        {
            await _client.CreateUserAsync(new CreateUserRequest
            {
                FullName = "User Two",
                Email = $"  {email.ToUpperInvariant()}  ",
                Password = "password123",
                Phone = "+222222",
                CountryCode = "DE",
                CustomerTier = CustomerTierProto.Standard,
            });
        };

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.AlreadyExists);
    }

    [Fact]
    public async Task UpdateUser_ShouldPersistNormalizedFields()
    {
        var created = await CreateUserAsync();

        var updated = await _client.UpdateUserAsync(new UpdateUserRequest
        {
            Id = created.Id,
            FullName = "  Updated User  ",
            Email = "  UPDATED-EMAIL@EXAMPLE.COM  ",
            Phone = "  +333333  ",
            CountryCode = " us ",
            CustomerTier = CustomerTierProto.Subscriber,
        });

        updated.Data.Should().NotBeNull();
        updated.Data.Id.Should().Be(created.Id);
        updated.Data.FullName.Should().Be("Updated User");
        updated.Data.Email.Should().Be("updated-email@example.com");
        updated.Data.Phone.Should().Be("+333333");
        updated.Data.CountryCode.Should().Be("US");
        updated.Data.CustomerTier.Should().Be(CustomerTierProto.Subscriber);

        var fetched = await _client.GetUserByIdAsync(new GetUserByIdRequest { Id = created.Id });
        fetched.Data.FullName.Should().Be("Updated User");
        fetched.Data.Email.Should().Be("updated-email@example.com");
        fetched.Data.CountryCode.Should().Be("US");
        fetched.Data.CustomerTier.Should().Be(CustomerTierProto.Subscriber);
    }

    [Fact]
    public async Task BlockUser_ShouldSetStatusToBlocked()
    {
        var created = await CreateUserAsync();

        var blocked = await _client.BlockUserAsync(new BlockUserRequest { Id = created.Id });

        blocked.Data.Should().NotBeNull();
        blocked.Data.Id.Should().Be(created.Id);
        blocked.Data.Status.Should().Be(UserStatusProto.Blocked);

        var fetched = await _client.GetUserByIdAsync(new GetUserByIdRequest { Id = created.Id });
        fetched.Data.Status.Should().Be(UserStatusProto.Blocked);
    }

    [Fact]
    public async Task UpdatePassword_ShouldValidateCurrentPassword_AndApplyChanges()
    {
        var created = await CreateUserAsync(password: "oldPassword123");

        Func<Task> wrongPassword = async () =>
        {
            await _client.UpdatePasswordAsync(new UpdatePasswordRequest
            {
                Id = created.Id,
                CurrentPassword = "wrongPassword",
                NewPassword = "newPassword123",
            });
        };

        var wrongPasswordEx = await wrongPassword.Should().ThrowAsync<RpcException>();
        wrongPasswordEx.Which.StatusCode.Should().Be(StatusCode.FailedPrecondition);

        await _client.UpdatePasswordAsync(new UpdatePasswordRequest
        {
            Id = created.Id,
            CurrentPassword = "oldPassword123",
            NewPassword = "newPassword123",
        });

        Func<Task> oldPasswordAfterUpdate = async () =>
        {
            await _client.UpdatePasswordAsync(new UpdatePasswordRequest
            {
                Id = created.Id,
                CurrentPassword = "oldPassword123",
                NewPassword = "anotherPassword123",
            });
        };

        var oldPasswordEx = await oldPasswordAfterUpdate.Should().ThrowAsync<RpcException>();
        oldPasswordEx.Which.StatusCode.Should().Be(StatusCode.FailedPrecondition);

        await _client.UpdatePasswordAsync(new UpdatePasswordRequest
        {
            Id = created.Id,
            CurrentPassword = "newPassword123",
            NewPassword = "anotherPassword123",
        });
    }

    [Fact]
    public async Task DeleteUser_ShouldBeIdempotent_AndGetShouldReturnNotFound()
    {
        var created = await CreateUserAsync();

        await _client.DeleteUserAsync(new DeleteUserRequest { Id = created.Id });

        Func<Task> getDeleted = async () =>
        {
            await _client.GetUserByIdAsync(new GetUserByIdRequest { Id = created.Id });
        };

        var getDeletedEx = await getDeleted.Should().ThrowAsync<RpcException>();
        getDeletedEx.Which.StatusCode.Should().Be(StatusCode.NotFound);

        Func<Task> deleteAgain = async () =>
        {
            await _client.DeleteUserAsync(new DeleteUserRequest { Id = created.Id });
        };

        await deleteAgain.Should().NotThrowAsync();
    }

    private async Task<UserProto> CreateUserAsync(
        string? email = null,
        string password = "password123")
    {
        var request = new CreateUserRequest
        {
            FullName = "John Doe",
            Email = email ?? $"user-{Guid.NewGuid():N}@example.com",
            Password = password,
            Phone = "+420123456",
            CountryCode = "DE",
            CustomerTier = CustomerTierProto.Standard,
        };

        var response = await _client.CreateUserAsync(request);
        return response.Data;
    }
}
