using Application.Common.Interfaces;
using Domain.Common.Interfaces;
using Domain.Entities;
using Domain.Gateways;
using Domain.Repositories;

namespace Application.UseCases.RefreshToken;

public class RefreshTokenUseCase
(IRefreshTokenRepository refreshTokenRepository,
    IIdGenerator idGenerator,
    IUserGateway userGateway,
    IJwtTokenGenerator jwtTokenGenerator) : IRefreshTokenUseCase
{

    private readonly int _accessTokenExpiresInSeconds = 3600;
    
    public async Task<RefreshTokenResponse> ExecuteAsync(RefreshTokenCommand command)
    {
        var refreshToken = await refreshTokenRepository.GetByTokenAsync(command.RefreshToken);

        if (refreshToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (refreshToken.IsRevoked)
        {
            throw new UnauthorizedAccessException("Refresh token has been revoked");
        }

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token has been expired");
        }

        var user = await userGateway.GetUserByIdAsync(refreshToken.UserId);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }
        
        if (user.Status == UserStatus.Blocked)
        {
            throw new UnauthorizedAccessException("User account is blocked");
        }

        var newAccessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
 
        var newRefreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshToken = new RefreshTokenEntity
        {
            Id = idGenerator.GenerateId(),
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await refreshTokenRepository.CreateAsync(newRefreshToken);

        await refreshTokenRepository.RevokeTokenAsync(command.RefreshToken, replacedByToken: newRefreshTokenValue);

        return new RefreshTokenResponse(newAccessToken, newRefreshTokenValue, _accessTokenExpiresInSeconds);

    }
}