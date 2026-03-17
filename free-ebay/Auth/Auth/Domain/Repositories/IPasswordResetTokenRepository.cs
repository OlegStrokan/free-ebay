using Domain.Entities;

namespace Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetTokenEntity?> GetByTokenAsync(string token);
    Task<List<PasswordResetTokenEntity>> GetByUserIdAsync(string userId);
    Task<PasswordResetTokenEntity> CreateAsync(PasswordResetTokenEntity resetTokenEntity);
    Task<PasswordResetTokenEntity> UpdateAsync(PasswordResetTokenEntity resetTokenEntity);
    Task MarkAsUsedAsync(string token);
    Task DeleteExpiredTokensAsync();
    Task DeleteByUserIdAsync(string userId);
}