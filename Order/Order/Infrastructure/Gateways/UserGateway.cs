using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Grpc.Core;
using Protos.User;
using StatusCode = Grpc.Core.StatusCode;

namespace Infrastructure.Gateways;

public sealed class UserGateway(
    UserServiceProto.UserServiceProtoClient client,
    ILogger<UserGateway> logger) : IUserGateway
{
    public async Task<UserProfileDto> GetUserProfileAsync(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetUserByIdAsync(
                new GetUserByIdRequest { Id = customerId.ToString() },
                cancellationToken: cancellationToken);

            var user = response.Data;
            if (user is null || string.IsNullOrWhiteSpace(user.Id))
            {
                throw new CustomerNotFoundException(customerId);
            }

            if (!Guid.TryParse(user.Id, out var parsedId))
            {
                throw new InvalidOperationException(
                    $"User service returned invalid customer id '{user.Id}'.");
            }

            var countryCode = string.IsNullOrWhiteSpace(user.CountryCode)
                ? "US"
                : user.CountryCode.Trim().ToUpperInvariant();

            var customerTier = user.CustomerTier switch
            {
                CustomerTierProto.Standard => "Standard",
                CustomerTierProto.Subscriber => "Subscriber",
                CustomerTierProto.Premium => "Premium",
                _ => "Standard"
            };

            var profile = new UserProfileDto(
                parsedId,
                user.Email,
                user.FullName,
                countryCode,
                customerTier,
                user.Status == UserStatusProto.Active);

            logger.LogDebug(
                "Resolved customer {CustomerId} profile from User service. Active={IsActive}",
                customerId,
                profile.IsActive);

            return profile;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new CustomerNotFoundException(customerId, ex);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new GatewayUnavailableException(
                $"User Service unavailable. gRPC={ex.StatusCode}: {ex.Status.Detail}",
                ex);
        }
    }
}
