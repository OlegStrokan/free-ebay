using Domain.Entities;

namespace Domain.Repositories;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByTokenAsync(string token);
    Task<EmailVerificationToken?> GetByUserIdAsync(string userId);
    Task<EmailVerificationToken> CreateAsync(EmailVerificationToken verificationToken);
    Task<EmailVerificationToken> UpdateAsync(EmailVerificationToken verificationToken);
    Task MarkAsUsedAsync(string token);
    Task DeletedExpiredTokensAsync();
    Task DeleteByUserIdAsync(string userId); 
}