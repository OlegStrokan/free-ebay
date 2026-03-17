using Domain.Entities.User;
using Protos.User;
using BlockUserResponse = Application.UseCases.BlockUser.BlockUserResponse;
using CreateUserResponse = Application.UseCases.CreateUser.CreateUserResponse;
using CreateUserResponseProto = Protos.User.CreateUserResponse;
using GetUserByIdResponse = Application.UseCases.GetUserById.GetUserByIdResponse;
using UpdateUserResponse = Application.UseCases.UpdateUser.UpdateUserResponse;
using BlockUserResponseProto = Protos.User.BlockUserResponse;
using GetUserByIdResponseProto = Protos.User.GetUserByIdResponse;
using UpdateUserResponseProto = Protos.User.UpdateUserResponse;


namespace Api.Mappers;

public static class UserMapper
{
    public static UserStatusProto ToProto(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => UserStatusProto.Active,
            UserStatus.Blocked => UserStatusProto.Blocked,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    public static UserStatus ToEntity(this UserStatusProto status)
    {
        return status switch
        {
            UserStatusProto.Active => UserStatus.Active,
            UserStatusProto.Blocked => UserStatus.Blocked,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    public static CustomerTierProto ToProto(this CustomerTier tier)
    {
        return tier switch
        {
            CustomerTier.Standard => CustomerTierProto.Standard,
            CustomerTier.Subscriber => CustomerTierProto.Subscriber,
            CustomerTier.Premium => CustomerTierProto.Premium,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
        };
    }

    public static CustomerTier ToEntity(this CustomerTierProto tier)
    {
        return tier switch
        {
            CustomerTierProto.Standard => CustomerTier.Standard,
            CustomerTierProto.Subscriber => CustomerTier.Subscriber,
            CustomerTierProto.Premium => CustomerTier.Premium,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
        };
    }

    public static CreateUserResponseProto ToProto(this CreateUserResponse response)
    {
        return new CreateUserResponseProto
        {
            Data = new UserProto
            {
                Id = response.Id,
                FullName = response.Fullname,
                Email = response.Email,
                Phone = response.Phone,
                Status = response.Status.ToProto(),
                CreatedAt = new DateTimeOffset(response.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = new DateTimeOffset(response.UpdatedAt).ToUnixTimeSeconds(),
                CountryCode = response.CountryCode,
                CustomerTier = response.CustomerTier.ToProto(),
            }
        };
    }

    public static CreateUserResponseProto ToProto(this CreateUserResponse response, string phone)
    {
        return new CreateUserResponseProto
        {
            Data = new UserProto
            {
                Id = response.Id,
                FullName = response.Fullname,
                Email = response.Email,
                Phone = phone ?? "",
                Status = response.Status.ToProto(),
                CreatedAt = new DateTimeOffset(response.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = new DateTimeOffset(response.UpdatedAt).ToUnixTimeSeconds(),
                CountryCode = response.CountryCode,
                CustomerTier = response.CustomerTier.ToProto(),
            }
        };
    }

    public static UserProto ToUserProto(this CreateUserResponse response)
    {
        return new UserProto
        {
            Id = response.Id,
            FullName = response.Fullname,
            Email = response.Email,
            Phone = response.Phone,
            Status = response.Status.ToProto(),
            CreatedAt = new DateTimeOffset(response.CreatedAt).ToUnixTimeSeconds(),
            UpdatedAt = new DateTimeOffset(response.UpdatedAt).ToUnixTimeSeconds(),
            CountryCode = response.CountryCode,
            CustomerTier = response.CustomerTier.ToProto(),
        };
    }

    public static GetUserByIdResponseProto ToProto(this GetUserByIdResponse? response)
    {
        if (response == null) return new GetUserByIdResponseProto { Data = null };

        return new GetUserByIdResponseProto
        {
            Data = new UserProto
            {
                Id = response.Id,
                FullName = response.Fullname,
                Email = response.Email,
                Phone = response.Phone,
                Status = response.Status.ToProto(),
                CreatedAt = new DateTimeOffset(response.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = new DateTimeOffset(response.UpdatedAt).ToUnixTimeSeconds(),
                CountryCode = response.CountryCode,
                CustomerTier = response.CustomerTier.ToProto(),
            }
        };
    }

    public static UpdateUserResponseProto ToProto(this UpdateUserResponse response)
    {
        return new UpdateUserResponseProto
        {
            Data =  new UserProto
            {
                Id = response.Id,
                FullName = response.Fullname,
                Email = response.Email,
                Phone = response.Phone,
                Status = response.Status.ToProto(),
                CreatedAt = new DateTimeOffset(response.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = new DateTimeOffset(response.UpdatedAt).ToUnixTimeSeconds(),
                CountryCode = response.CountryCode,
                CustomerTier = response.CustomerTier.ToProto(),
            }
        };
    }

    public static BlockUserResponseProto ToProto(this BlockUserResponse response)
    {
        return new BlockUserResponseProto
        {
            Data = new UserProto
            {
                Id = response.Id,
                FullName = response.Fullname,
                Email = response.Email,
                Phone = response.Phone,
                Status = response.Status.ToProto(),
                CreatedAt = new DateTimeOffset(response.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = new DateTimeOffset(response.UpdatedAt).ToUnixTimeSeconds(),
                CountryCode = response.CountryCode,
                CustomerTier = response.CustomerTier.ToProto(),
            }
        };
    }

    public static UserProto ToProto(this UserEntity entity)
    {
        return new UserProto
        {
            Id = entity.Id.ToString(),
            FullName = entity.Fullname,
            Email = entity.Email,
            Phone = entity.Phone ?? "",
            Status = entity.Status.ToProto(),
            CreatedAt = new DateTimeOffset(entity.CreatedAt).ToUnixTimeSeconds(),
            UpdatedAt = new DateTimeOffset(entity.UpdatedAt).ToUnixTimeSeconds(),
            CountryCode = entity.CountryCode,
            CustomerTier = entity.CustomerTier.ToProto(),
        };
    }

    public static CreateUserResponseProto ToCreateUserResponseProto(this UserEntity entity)
    {
        return new CreateUserResponseProto
        {
            Data = entity.ToProto()
        };
    }
}