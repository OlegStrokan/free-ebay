using Domain.Repositories;

namespace Application.UseCases.RevokeToken;

public class RevokeTokenUseCase(IRefreshTokenRepository refreshTokenRepository)
{
    public async Task<RevokeTokenResponse> ExecuteAsync(RevokeTokenCommand command)
    {
        var refreshToken = await refreshTokenRepository.GetByTokenAsync(command.RefreshToken);

        if (refreshToken == null)
        {
            return new RevokeTokenResponse(false, "Refresh token not found");
        }

        if (refreshToken.IsRevoked)
        {
            return new RevokeTokenResponse(false, "Refresh token already revoked");
        }

        await refreshTokenRepository.RevokeTokenAsync(command.RefreshToken, command.RevokedById);

        return new RevokeTokenResponse(true, "Refresh token revoked");
    }
}