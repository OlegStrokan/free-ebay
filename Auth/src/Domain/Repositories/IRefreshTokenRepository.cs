using Domain.Entities;

namespace Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string refreshToken);
    Task<RefreshToken?> GetByIdAsync(string id);
    Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(string userId);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken> UpdateAsync(RefreshToken refreshToken);
    Task RevokeTokenAsync(string token, string? revokedById = null, string? replacedByToken = null);
    Task RevokeAllUserTokensAsync(string userId, string? revokedById = null);
    Task DeletedExpiredTokensAsync();

}