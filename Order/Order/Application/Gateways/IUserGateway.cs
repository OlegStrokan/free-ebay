using Application.DTOs;

namespace Application.Gateways;

public interface IUserGateway
{
    Task<UserProfileDto> GetUserProfileAsync(Guid customerId, CancellationToken cancellationToken);
}
