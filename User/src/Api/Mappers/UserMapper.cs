using Domain.Entities.User;
using Protos.User;
using CreateUserResponse = Application.UseCases.CreateUser.CreateUserResponse;
using CreateUserResponseProto = Protos.User.CreateUserResponse;
using GetUserByIdResponse = Application.UseCases.GetUserById.GetUserByIdResponse;
using UpdateUserResponse = Application.UseCases.UpdateUser.UpdateUserResponse;
using GetUserByIdResponseProto = Protos.User.GetUserByIdResponse;
using UpdateUserResponseProto = Protos.User.UpdateUserResponse;


namespace Api.Mappers;

public static class UserMapper
{
    // ========================================================================
    // STATUS MAPPINGS
    // ========================================================================
    
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

    // ========================================================================
    // MAIN MAPPING: CreateUserResponse -> CreateUserResponseProto
    // THIS IS WHAT YOU NEED FOR YOUR SERVICE!
    // ========================================================================
    

    public static CreateUserResponseProto ToProto(this CreateUserResponse response)
    {
        return new CreateUserResponseProto
        {
            Data = new UserProto
            {
                Id = response.Id,
                FullName = response.Fullname,
                Email = response.Email,
                Phone = "", // CreateUserResponse doesn't have phone, set empty or handle as needed
                Status = response.Status.ToProto(),
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };
    }

    // ========================================================================
    // ALTERNATIVE: If you want to pass phone separately
    // ========================================================================

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
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };
    }

    // ========================================================================
    // HELPER: CreateUserResponse -> UserProto (if you need just UserProto)
    // ========================================================================
    

    public static UserProto ToUserProto(this CreateUserResponse response)
    {
        return new UserProto
        {
            Id = response.Id,
            FullName = response.Fullname,
            Email = response.Email,
            Phone = "",
            Status = response.Status.ToProto(),
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    // could be NULL
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
                Status = response.Status.ToProto()
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
                Status = response.Status.ToProto()
            }
        };
    }
    
    

    // ========================================================================
    // FULL USER ENTITY MAPPINGS (for other use cases)
    // ========================================================================
    

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
            UpdatedAt = new DateTimeOffset(entity.UpdatedAt).ToUnixTimeSeconds()
        };
    }


    public static CreateUserResponseProto ToCreateUserResponseProto(this UserEntity entity)
    {
        return new CreateUserResponseProto
        {
            Data = entity.ToProto()
        };
    }

    // ========================================================================
    // DELIVERY INFO MAPPINGS (commented out, uncomment when needed)
    // ========================================================================
    
    /*
    public static DeliveryInfoProto ToProto(this DeliveryInfo deliveryInfo)
    {
        return new DeliveryInfoProto
        {
            Id = deliveryInfo.Id.ToString(),
            Street = deliveryInfo.Street,
            City = deliveryInfo.City,
            CountryDestination = deliveryInfo.CountryDestination,
            PostalCode = deliveryInfo.PostalCode
        };
    }

    public static DeliveryInfo ToEntity(this DeliveryInfoProto proto, string userId)
    {
        return new DeliveryInfo(
            street: proto.Street,
            city: proto.City,
            postalCode: proto.PostalCode,
            countryDestination: proto.CountryDestination,
            userId: userId
        );
    }
    */
}