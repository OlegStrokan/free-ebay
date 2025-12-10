using Domain.Entities;

namespace Domain.Repositories;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationTokenEntity?> GetByTokenAsync(string token);
    Task<EmailVerificationTokenEntity?> GetByUserIdAsync(string userId);
    Task<EmailVerificationTokenEntity> CreateAsync(EmailVerificationTokenEntity verificationTokenEntity);
    Task<EmailVerificationTokenEntity> UpdateAsync(EmailVerificationTokenEntity verificationTokenEntity);
    Task MarkAsUsedAsync(string token);
    Task DeletedExpiredTokensAsync();
    Task DeleteByUserIdAsync(string userId); 
}