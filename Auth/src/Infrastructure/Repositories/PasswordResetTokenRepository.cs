using Domain.Entities;
using Domain.Repositories;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PasswordResetTokenRepository(AppDbContext dbContext) : IPasswordResetTokenRepository
{
    public async Task<PasswordResetToken?> GetByTokenAsync(string token)
    {
        return await dbContext.PasswordResetTokens.FirstOrDefaultAsync(prt => prt.Token == token);
    }

    public async Task<List<PasswordResetToken>> GetByUserIdAsync(string userId)
    {
        return await dbContext.PasswordResetTokens.Where(prt =>
                prt.UserId == userId &&
                !prt.IsUsed &&
                prt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(prt => prt.CreatedAt)
            .ToListAsync();
    }

    public async Task<PasswordResetToken> CreateAsync(PasswordResetToken resetToken)
    {
        dbContext.Add(resetToken);
        await dbContext.SaveChangesAsync();
        return resetToken;
    }

    public async Task<PasswordResetToken> UpdateAsync(PasswordResetToken resetToken)
    {
        dbContext.Update(resetToken);
        await dbContext.SaveChangesAsync();
        return resetToken;
    }

    public async Task MarkAsUsedAsync(string token)
    {
        var resetToken = await GetByTokenAsync(token);

        if (resetToken == null)
        {
            throw new InvalidOperationException($"Password reset token {token} not found");
        }

        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;

        await UpdateAsync(resetToken);
    }

    public async Task DeleteExpiredTokensAsync()
    {
        // var expiredTokens = await dbContext.PasswordResetTokens.Where(prt =>
        //         prt.ExpiresAt < DateTime.UtcNow || prt.IsUsed)
        //         .ToListAsync();
        //
        // dbContext.PasswordResetTokens.RemoveRange(expiredTokens);
        // await dbContext.SaveChangesAsync();
        
        // despacio? 
        
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM PasswordResetTokens WHERE ExpiresAt < {DateTime.UtcNow} Or IsUsed = {true}");
    }

    public async Task DeleteByUserIdAsync(string userId)
    {
        var tokens = await dbContext.PasswordResetTokens.Where(prt => prt.UserId == userId).ToListAsync();
    }
}