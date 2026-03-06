using Domain.Gateways;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Protos.User;

namespace Infrastructure.Gateways;


/// <summary>
/// infa implementation of user microservice
/// clean as fuck
/// </summary>
/// <param name="userClient"></param>

public class UserGateway
    (UserServiceProto.UserServiceProtoClient userClient, ILogger<UserGateway> logger) : IUserGateway
{
    public async Task<string> CreateUserAsync(string email, string hashedPassword, string fullName, string phone)
    {
        try
        {
            logger.LogInformation("Creating user via User microservice: {Email}", email);

            var request = new CreateUserRequest
            {
                Email = email,
                Password = hashedPassword,
                FullName = fullName,
                Phone = phone
            };

            var response = await userClient.CreateUserAsync(request);

            // @todo: add some status or indicator of success

            if (response.Data == null)
            {
                // @todo: it's very shitty description, oukey?
                throw new InvalidOperationException("User microservice returned null user");
            }
            
            logger.LogInformation("User created successfully: {UserId}", response.Data.Id);
            return response.Data.Id;

        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "gRPC error creating user: {Status}", ex.StatusCode);
            throw new InvalidOperationException($"Failed to create user: {ex.Status.Detail}", ex);
        }
    }

    public async Task<UserGatewayDto?> GetUserByEmailAsync(string email)
    {
        try
        {
            logger.LogInformation("Getting user email via User microservice: {Email}", email);

            var request = new GetUserByEmailRequest { Email = email };
            var response = await userClient.GetUserByEmailAsync(request);


            if (response.Data == null)
            {
                logger.LogWarning("User not found: {Email}", email);
                return null;
            }

            return MapToGatewayDto(response);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            logger.LogInformation("User not found: {Email}", email);
            return null;
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "gRPC error getting user: {Status}", ex.StatusCode);
            throw new InvalidOperationException($"Failed to get user: {ex.Status.Detail}", ex);
        }

    }

    public async Task<UserGatewayDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            logger.LogInformation("Getting user via ID vie User microservice: {UserId}", userId);

            var request = new GetUserByIdRequest { Id = userId };
            var response = await userClient.GetUserByIdAsync(request);

            // @think: we're checking if data == null + additionally check rpc not found exception. so much "ting"
            if (response.Data == null)
            {
                logger.LogWarning("User not found: {UserId}", userId);
                return null;
            }

            return new UserGatewayDto
            {
                Id = response.Data.Id,
                Email = response.Data.Email,
                FullName = response.Data.FullName,
                Phone = response.Data.Phone,
                Status = MapUserStatus(response.Data.Status),
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            logger.LogInformation("User not found: {UserId}", userId);
            return null;
        }

        catch (RpcException ex)
        {
            logger.LogError(ex,  "gRPC error getting user: {Status}", ex.StatusCode);
            throw new InvalidOperationException($"Failed to get user: {ex.Status.Detail}", ex);
        }
    }

    public async Task<bool> VerifyUserEmailAsync(string userId)
    {
        try
        {
            logger.LogInformation("Verifying user email via User microservice: {UserId}", userId);

            var request = new VerifyUserEmailRequest { UserId = userId };
            var response = await userClient.VerifyUserEmailAsync(request);

            logger.LogInformation("Email verification result: {Success}", response.Success);
            return response.Success;
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "gRPC error varifying email {Status}", ex.StatusCode);
            throw new InvalidOperationException($"Failed to verify user: {ex.Status.Detail}", ex);
        }
    }

    public async Task<bool> UpdateUserPasswordAsync(string userId, string newPasswordHash)
    {
        try
        {
            logger.LogInformation("Updating user password via User microservice: {UserId}", userId);

            var request = new UpdateUserPasswordRequest
            {
                UserId = userId,
                NewPasswordHash = newPasswordHash
            };

            var response = await userClient.UpdateUserPasswordAsync(request);

            logger.LogInformation("Password update result: {Success}", response.Success);
            return response.Success;
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "gRPC error update user: {Status}", ex.StatusCode);
            throw new InvalidOperationException($"Failed to update password: {ex.Status.Detail}");
        }
    }

    private UserGatewayDto MapToGatewayDto(GetUserByEmailResponse user)
    {
        // @todo: go get some automapper
        return new UserGatewayDto
        {
            Id = user.Data.Id,
            Email = user.Data.Id,
            FullName = user.Data.FullName,
            Phone = user.Data.Phone,
            PasswordHash = user.PasswordHash,
            Status = MapUserStatus(user.Data.Status)
        };
    }

    private UserStatus MapUserStatus(UserStatusProto userStatus)
    {
        return userStatus switch
        {
            UserStatusProto.Active => UserStatus.Active,
            UserStatusProto.Blocked => UserStatus.Blocked,
            _ => UserStatus.Active
        };
    }
}
