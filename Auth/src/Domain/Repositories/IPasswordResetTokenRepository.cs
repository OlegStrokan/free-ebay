using Domain.Entities;

namespace Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task<List<PasswordResetToken>> GetByUserIdAsync(string userId);
    Task<PasswordResetToken> CreateAsync(PasswordResetToken resetToken);
    Task<PasswordResetToken> UpdateAsync(PasswordResetToken resetToken);
    Task MarkAsUsedAsync(string token);
    Task DeleteExpiredTokensAsync();
    Task DeleteByUserIdAsync(string userId);
}