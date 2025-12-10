using Domain.Entities;

namespace Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenEntity?> GetByTokenAsync(string refreshToken);
    Task<RefreshTokenEntity?> GetByIdAsync(string id);
    Task<List<RefreshTokenEntity>> GetActiveTokensByUserIdAsync(string userId);
    Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshTokenEntity);
    Task<RefreshTokenEntity> UpdateAsync(RefreshTokenEntity refreshTokenEntity);
    Task RevokeTokenAsync(string token, string? revokedById = null, string? replacedByToken = null);
    Task RevokeAllUserTokensAsync(string userId, string? revokedById = null);
    Task DeletedExpiredTokensAsync();

}