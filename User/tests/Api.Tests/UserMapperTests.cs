using Api.Mappers;
using Domain.Entities.User;
using Protos.User;
using CreateUserResponse = Application.UseCases.CreateUser.CreateUserResponse;
using GetUserByIdResponse = Application.UseCases.GetUserById.GetUserByIdResponse;
using UpdateUserResponse = Application.UseCases.UpdateUser.UpdateUserResponse;

namespace Api.Tests;

public class UserMapperTests
{

    [Fact]
    public void ToProto_ShouldMapActiveStatus()
    {
        var status = UserStatus.Active;
        
        var result = status.ToProto();

        Assert.Equal(UserStatusProto.Active, result);
    }

    [Fact]
    public void ToProto_ShouldMapBlockedStatus()
    {
        var status = UserStatus.Blocked;

        var result = status.ToProto();

        Assert.Equal(UserStatusProto.Blocked, result);
    }

    [Fact]
    public void ToEntity_ShouldMapActiveStatus()
    {
        var status = UserStatusProto.Active;

        var result = status.ToEntity();

        Assert.Equal(UserStatus.Active, result);
    }

    [Fact]
    public void ToEntity_ShouldMapBlockedStatus()
    {
        var status = UserStatusProto.Blocked;

        var result = status.ToEntity();

        Assert.Equal(UserStatus.Blocked, result);
    }
    

    [Fact]
    public void CreateUserResponse_ToProto_ShouldMapCorrectly()
    {
        var response = new CreateUserResponse(
            "user_123",
            "test@example.com",
            "John Doe",
            UserStatus.Active
        );

        var result = response.ToProto();

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("test@example.com", result.Data.Email);
        Assert.Equal("John Doe", result.Data.FullName);
        Assert.Equal("", result.Data.Phone); // Phone is empty in this overload
        Assert.Equal(UserStatusProto.Active, result.Data.Status);
        Assert.True(result.Data.CreatedAt > 0);
        Assert.True(result.Data.UpdatedAt > 0);
    }

    [Fact]
    public void CreateUserResponse_ToProtoWithPhone_ShouldMapCorrectly()
    {
        var response = new CreateUserResponse(
            "user_123",
            "test@example.com",
            "John Doe",
            UserStatus.Active
        );
        var phone = "+1234567890";
        
        var result = response.ToProto(phone);
        
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("test@example.com", result.Data.Email);
        Assert.Equal("John Doe", result.Data.FullName);
        Assert.Equal("+1234567890", result.Data.Phone);
        Assert.Equal(UserStatusProto.Active, result.Data.Status);
    }

    [Fact]
    public void CreateUserResponse_ToUserProto_ShouldMapCorrectly()
    {

        var response = new CreateUserResponse(
            "user_123",
            "test@example.com",
            "John Doe",
            UserStatus.Blocked
        );
        
        var result = response.ToUserProto();
        
        Assert.Equal("user_123", result.Id);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("", result.Phone);
        Assert.Equal(UserStatusProto.Blocked, result.Status);
    }
    
    [Fact]
    public void GetUserByIdResponse_ToProto_ShouldMapCorrectly()
    {
        var response = new GetUserByIdResponse(
            "user_123",
            "test@example.com",
            "Jane Doe",
            "+9876543210",
            UserStatus.Active
        );
        
        var result = response.ToProto();
        
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("test@example.com", result.Data.Email);
        Assert.Equal("Jane Doe", result.Data.FullName);
        Assert.Equal("+9876543210", result.Data.Phone);
        Assert.Equal(UserStatusProto.Active, result.Data.Status);
    }

    [Fact]
    public void GetUserByIdResponse_ToProto_ShouldReturnEmptyData_WhenNull()
    {
        GetUserByIdResponse? response = null;
        
        var result = response.ToProto();
        
        Assert.NotNull(result);
        Assert.Null(result.Data);
    }

    [Fact]
    public void UpdateUserResponse_ToProto_ShouldMapCorrectly()
    {
        var response = new UpdateUserResponse(
            "user_123",
            "updated@example.com",
            "Updated Name",
            "+1111111111",
            UserStatus.Blocked
        );
        var result = response.ToProto();
        
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("updated@example.com", result.Data.Email);
        Assert.Equal("Updated Name", result.Data.FullName);
        Assert.Equal("+1111111111", result.Data.Phone);
        Assert.Equal(UserStatusProto.Blocked, result.Data.Status);
    }
    
    [Fact]
    public void UserEntity_ToProto_ShouldMapCorrectly()
    {
        var entity = new UserEntity
        {
            Id = "user_123",
            Email = "entity@example.com",
            Fullname = "Entity User",
            Phone = "+5555555555",
            Password = "password",
            Status = UserStatus.Active,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var result = entity.ToProto();

        // Assert
        Assert.Equal("user_123", result.Id);
        Assert.Equal("entity@example.com", result.Email);
        Assert.Equal("Entity User", result.FullName);
        Assert.Equal("+5555555555", result.Phone);
        Assert.Equal(UserStatusProto.Active, result.Status);
        Assert.True(result.CreatedAt > 0);
        Assert.True(result.UpdatedAt > 0);
    }

    [Fact]
    public void UserEntity_ToProto_ShouldHandleNullPhone()
    {
        var entity = new UserEntity
        {
            Id = "user_123",
            Email = "test@example.com",
            Fullname = "Test User",
            Phone = null,
            Password = "password",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var result = entity.ToProto();
        
        Assert.Equal("", result.Phone); // Null becomes empty string
    }

    [Fact]
    public void UserEntity_ToCreateUserResponseProto_ShouldMapCorrectly()
    {
        var entity = new UserEntity
        {
            Id = "user_123",
            Email = "test@example.com",
            Fullname = "Test User",
            Phone = "+1234567890",
            Password = "password",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = entity.ToCreateUserResponseProto();

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("test@example.com", result.Data.Email);
        Assert.Equal("Test User", result.Data.FullName);
        Assert.Equal("+1234567890", result.Data.Phone);
    }
}