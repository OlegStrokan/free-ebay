using Api.Mappers;
using Application.Dtos;
using Domain.Entities.DeliveryInfo;
using Domain.Entities.User;
using Protos.User;
using BlockUserResponse = Application.UseCases.BlockUser.BlockUserResponse;
using CreateUserResponse = Application.UseCases.CreateUser.CreateUserResponse;
using GetUserByEmailResponse = Application.UseCases.GetUserByEmail.GetUserByEmailResponse;
using GetUserByIdResponse = Application.UseCases.GetUserById.GetUserByIdResponse;
using UpdateUserResponse = Application.UseCases.UpdateUser.UpdateUserResponse;
using VerifyCredentialsResponse = Application.UseCases.VerifyCredentials.VerifyCredentialsResponse;

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
    public void CustomerTier_ToProto_ShouldMapPremium()
    {
        var tier = CustomerTier.Premium;

        var result = tier.ToProto();

        Assert.Equal(CustomerTierProto.Premium, result);
    }

    [Fact]
    public void CustomerTier_ToEntity_ShouldMapSubscriber()
    {
        var tier = CustomerTierProto.Subscriber;

        var result = tier.ToEntity();

        Assert.Equal(CustomerTier.Subscriber, result);
    }

    [Fact]
    public void CreateUserResponse_ToProto_ShouldMapCorrectly()
    {
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);

        var response = new CreateUserResponse(
            "user_123",
            "test@example.com",
            "John Doe",
            "+1234567890",
            "DE",
            CustomerTier.Standard,
            UserStatus.Active,
            createdAt,
            updatedAt,
            true
        );

        var result = response.ToProto();

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("test@example.com", result.Data.Email);
        Assert.Equal("John Doe", result.Data.FullName);
        Assert.Equal("+1234567890", result.Data.Phone);
        Assert.Equal("DE", result.Data.CountryCode);
        Assert.Equal(CustomerTierProto.Standard, result.Data.CustomerTier);
        Assert.Equal(UserStatusProto.Active, result.Data.Status);
        Assert.True(result.Data.IsEmailVerified);
        Assert.True(result.Data.CreatedAt > 0);
        Assert.True(result.Data.UpdatedAt > 0);
    }

    [Fact]
    public void CreateUserResponse_ToProtoWithPhone_ShouldMapCorrectly()
    {
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);

        var response = new CreateUserResponse(
            "user_123",
            "test@example.com",
            "John Doe",
            "+1111111111",
            "DE",
            CustomerTier.Standard,
            UserStatus.Active,
            createdAt,
            updatedAt,
            false
        );

        var phone = "+1234567890";

        var result = response.ToProto(phone);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("test@example.com", result.Data.Email);
        Assert.Equal("John Doe", result.Data.FullName);
        Assert.Equal("+1234567890", result.Data.Phone);
        Assert.Equal("DE", result.Data.CountryCode);
        Assert.Equal(CustomerTierProto.Standard, result.Data.CustomerTier);
        Assert.Equal(UserStatusProto.Active, result.Data.Status);
        Assert.False(result.Data.IsEmailVerified);
    }

    [Fact]
    public void CreateUserResponse_ToUserProto_ShouldMapCorrectly()
    {
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);

        var response = new CreateUserResponse(
            "user_123",
            "test@example.com",
            "John Doe",
            "+420000000",
            "US",
            CustomerTier.Premium,
            UserStatus.Blocked,
            createdAt,
            updatedAt,
            true
        );

        var result = response.ToUserProto();

        Assert.Equal("user_123", result.Id);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("+420000000", result.Phone);
        Assert.Equal("US", result.CountryCode);
        Assert.Equal(CustomerTierProto.Premium, result.CustomerTier);
        Assert.Equal(UserStatusProto.Blocked, result.Status);
        Assert.True(result.IsEmailVerified);
    }

    [Fact]
    public void GetUserByEmailResponse_ToProto_ShouldMapCorrectly()
    {
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);

        var response = new GetUserByEmailResponse(
            "user_123",
            "email@example.com",
            "Email User",
            "+444444",
            "US",
            CustomerTier.Subscriber,
            UserStatus.Active,
            createdAt,
            updatedAt,
            true);

        var result = response.ToProto();

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("email@example.com", result.Data.Email);
        Assert.Equal("Email User", result.Data.FullName);
        Assert.Equal("+444444", result.Data.Phone);
        Assert.Equal("US", result.Data.CountryCode);
        Assert.Equal(CustomerTierProto.Subscriber, result.Data.CustomerTier);
        Assert.Equal(UserStatusProto.Active, result.Data.Status);
        Assert.True(result.Data.IsEmailVerified);
    }

    [Fact]
    public void GetUserByEmailResponse_ToProto_ShouldReturnEmpty_WhenNull()
    {
        GetUserByEmailResponse? response = null;

        var result = response.ToProto();

        Assert.NotNull(result);
        Assert.Null(result.Data);
    }

    [Fact]
    public void VerifyCredentialsResponse_ToProto_ShouldMapCorrectly()
    {
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);

        var response = new VerifyCredentialsResponse(
            "user_123",
            "email@example.com",
            "Email User",
            "+444444",
            "US",
            CustomerTier.Subscriber,
            UserStatus.Active,
            createdAt,
            updatedAt,
            true);

        var result = response.ToProto();

        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("email@example.com", result.Data.Email);
        Assert.Equal("Email User", result.Data.FullName);
    }

    [Fact]
    public void VerifyCredentialsResponse_ToProto_ShouldReturnInvalid_WhenNull()
    {
        VerifyCredentialsResponse? response = null;

        var result = response.ToProto();

        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Null(result.Data);
    }

    [Fact]
    public void GetUserByIdResponse_ToProto_ShouldMapCorrectly()
    {
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);

        var response = new GetUserByIdResponse(
            "user_123",
            "test@example.com",
            "Jane Doe",
            "+9876543210",
            "DE",
            CustomerTier.Subscriber,
            UserStatus.Active,
            createdAt,
            updatedAt,
            true
        );

        var result = response.ToProto();

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("test@example.com", result.Data.Email);
        Assert.Equal("Jane Doe", result.Data.FullName);
        Assert.Equal("+9876543210", result.Data.Phone);
        Assert.Equal("DE", result.Data.CountryCode);
        Assert.Equal(CustomerTierProto.Subscriber, result.Data.CustomerTier);
        Assert.Equal(UserStatusProto.Active, result.Data.Status);
        Assert.True(result.Data.IsEmailVerified);
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
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);

        var response = new UpdateUserResponse(
            "user_123",
            "updated@example.com",
            "Updated Name",
            "+1111111111",
            "US",
            CustomerTier.Premium,
            UserStatus.Blocked,
            createdAt,
            updatedAt,
            true
        );

        var result = response.ToProto();

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("updated@example.com", result.Data.Email);
        Assert.Equal("Updated Name", result.Data.FullName);
        Assert.Equal("+1111111111", result.Data.Phone);
        Assert.Equal("US", result.Data.CountryCode);
        Assert.Equal(CustomerTierProto.Premium, result.Data.CustomerTier);
        Assert.Equal(UserStatusProto.Blocked, result.Data.Status);
        Assert.True(result.Data.IsEmailVerified);
    }

    [Fact]
    public void BlockUserResponse_ToProto_ShouldMapCorrectly()
    {
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddMinutes(1);

        var response = new BlockUserResponse(
            "user_123",
            "blocked@example.com",
            "Blocked User",
            "+121212121",
            "DE",
            CustomerTier.Standard,
            UserStatus.Blocked,
            createdAt,
            updatedAt,
            true);

        var result = response.ToProto();

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal("user_123", result.Data.Id);
        Assert.Equal("blocked@example.com", result.Data.Email);
        Assert.Equal(UserStatusProto.Blocked, result.Data.Status);
        Assert.Equal("DE", result.Data.CountryCode);
        Assert.Equal(CustomerTierProto.Standard, result.Data.CustomerTier);
        Assert.True(result.Data.IsEmailVerified);
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
            CountryCode = "DE",
            CustomerTier = CustomerTier.Subscriber,
            IsEmailVerified = true,
            Password = "password",
            Status = UserStatus.Active,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = entity.ToProto();

        Assert.Equal("user_123", result.Id);
        Assert.Equal("entity@example.com", result.Email);
        Assert.Equal("Entity User", result.FullName);
        Assert.Equal("+5555555555", result.Phone);
        Assert.Equal("DE", result.CountryCode);
        Assert.Equal(CustomerTierProto.Subscriber, result.CustomerTier);
        Assert.Equal(UserStatusProto.Active, result.Status);
        Assert.True(result.IsEmailVerified);
        Assert.True(result.CreatedAt > 0);
        Assert.True(result.UpdatedAt > 0);
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
            CountryCode = "DE",
            CustomerTier = CustomerTier.Standard,
            IsEmailVerified = true,
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
        Assert.True(result.Data.IsEmailVerified);
    }

    [Fact]
    public void DeliveryInfoDto_ToProto_ShouldMapAllFields()
    {
        var dto = new DeliveryInfoDto("di-1", "Main St 1", "Prague", "11000", "CZ");

        var result = dto.ToProto();

        Assert.Equal("di-1", result.Id);
        Assert.Equal("Main St 1", result.Street);
        Assert.Equal("Prague", result.City);
        Assert.Equal("11000", result.PostalCode);
        Assert.Equal("CZ", result.CountryDestination);
    }

    [Fact]
    public void DeliveryInfoEntity_ToProto_ShouldMapAllFields()
    {
        var entity = new DeliveryInfo
        {
            Id = "di-2",
            UserId = "user-1",
            Street = "Oak Ave 5",
            City = "Berlin",
            PostalCode = "10115",
            CountryDestination = "DE",
        };

        var result = entity.ToProto();

        Assert.Equal("di-2", result.Id);
        Assert.Equal("Oak Ave 5", result.Street);
        Assert.Equal("Berlin", result.City);
        Assert.Equal("10115", result.PostalCode);
        Assert.Equal("DE", result.CountryDestination);
    }

    [Fact]
    public void CreateUserResponse_ToProto_ShouldIncludeDeliveryInfos()
    {
        var createdAt = DateTime.UtcNow;
        var response = new CreateUserResponse(
            "user_123",
            "test@example.com",
            "John Doe",
            "+1234567890",
            "DE",
            CustomerTier.Standard,
            UserStatus.Active,
            createdAt,
            createdAt,
            false,
            [new DeliveryInfoDto("di-1", "Main St 1", "Prague", "11000", "CZ")]
        );

        var result = response.ToProto();

        Assert.Single(result.Data.DeliveryInfo);
        Assert.Equal("di-1", result.Data.DeliveryInfo[0].Id);
        Assert.Equal("Main St 1", result.Data.DeliveryInfo[0].Street);
        Assert.Equal("Prague", result.Data.DeliveryInfo[0].City);
        Assert.Equal("11000", result.Data.DeliveryInfo[0].PostalCode);
        Assert.Equal("CZ", result.Data.DeliveryInfo[0].CountryDestination);
    }

    [Fact]
    public void CreateUserResponse_ToProto_ShouldHaveEmptyDeliveryInfo_WhenNull()
    {
        var createdAt = DateTime.UtcNow;
        var response = new CreateUserResponse(
            "user_123",
            "test@example.com",
            "John Doe",
            "+1234567890",
            "DE",
            CustomerTier.Standard,
            UserStatus.Active,
            createdAt,
            createdAt,
            false
        );

        var result = response.ToProto();

        Assert.Empty(result.Data.DeliveryInfo);
    }

    [Fact]
    public void UserEntity_ToProto_ShouldIncludeDeliveryInfos()
    {
        var entity = new UserEntity
        {
            Id = "user_123",
            Email = "entity@example.com",
            Fullname = "Entity User",
            Phone = "+5555555555",
            CountryCode = "DE",
            CustomerTier = CustomerTier.Standard,
            IsEmailVerified = false,
            Password = "password",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeliveryInfos =
            [
                new DeliveryInfo { Id = "di-3", UserId = "user_123", Street = "Pine Rd 9", City = "Vienna", PostalCode = "1010", CountryDestination = "AT" },
            ],
        };

        var result = entity.ToProto();

        Assert.Single(result.DeliveryInfo);
        Assert.Equal("di-3", result.DeliveryInfo[0].Id);
        Assert.Equal("Pine Rd 9", result.DeliveryInfo[0].Street);
        Assert.Equal("Vienna", result.DeliveryInfo[0].City);
        Assert.Equal("1010", result.DeliveryInfo[0].PostalCode);
        Assert.Equal("AT", result.DeliveryInfo[0].CountryDestination);
    }
}