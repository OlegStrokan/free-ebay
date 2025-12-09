using Domain.Entities;
using Domain.Repositories;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EmailVerificationTokenRepository(AppDbContext dbContext): IEmailVerificationTokenRepository
{
    public async Task<EmailVerificationToken?> GetByTokenAsync(string token)
    {
        return await dbContext.EmailVerificationTokens.FirstOrDefaultAsync(evt => evt.Token == token);
    }

    public async Task<EmailVerificationToken?> GetByUserIdAsync(string userId)
    {
        return await dbContext.EmailVerificationTokens.Where(evt =>
            evt.UserId == userId &&
            !evt.IsUsed &&
            evt.ExpiresAt > DateTime.UtcNow).FirstOrDefaultAsync();
    }

    public async Task<EmailVerificationToken> CreateAsync(EmailVerificationToken verificationToken)
    {
        dbContext.Add(verificationToken);
        await dbContext.SaveChangesAsync();
        return verificationToken;
    }

    public async Task<EmailVerificationToken> UpdateAsync(EmailVerificationToken verificationToken)
    {
        dbContext.Update(verificationToken);
        await dbContext.SaveChangesAsync();
        return verificationToken;
    }

    public async Task MarkAsUsedAsync(string token)
    {
        var emailVerificationToken = await GetByTokenAsync(token);

        if (emailVerificationToken == null)
        {
            throw new InvalidOperationException($"Email verification token with Token {token} not found");
        }

        emailVerificationToken.IsUsed = true;
        emailVerificationToken.UsedAt = DateTime.UtcNow;
        
        await UpdateAsync(emailVerificationToken);
        
    }

    public async Task DeletedExpiredTokensAsync()
    {
        // var expiredTokens = await dbContext.EmailVerificationTokens.Where(evt =>
        //     evt.ExpiresAt < DateTime.UtcNow
        //     || evt.IsUsed
        // ).ToListAsync();
        //
        // await dbContext.SaveChangesAsync();
        
        // despacio again?
        
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM EmailVerificationToken WHERE ExpiresAt < {DateTime.UtcNow} Or IsUsed = {true}");
    }

    public async Task DeleteByUserIdAsync(string userId)
    {
        var tokens = await dbContext.EmailVerificationTokens.Where(evt => evt.UserId == userId).ToListAsync();
    }
}