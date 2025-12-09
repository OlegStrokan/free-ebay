using Domain.Entities;
using Domain.Repositories;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext dbContext) : IRefreshTokenRepository

{
    public async Task<RefreshToken?> GetByTokenAsync(string refreshToken)
    {
        return await dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
    }

    public async Task<RefreshToken?> GetByIdAsync(string id)
    {
        return await dbContext.RefreshTokens.FindAsync(id);
    }

    public Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(string userId)
    {
        return dbContext.RefreshTokens.Where(rt =>
                rt.UserId == userId && rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync<RefreshToken>();
        
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        dbContext.Add(refreshToken);
        await dbContext.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<RefreshToken> UpdateAsync(RefreshToken refreshToken)
    {
        dbContext.Update(refreshToken);
        await dbContext.SaveChangesAsync();
        return refreshToken;
    }

    public async Task RevokeTokenAsync(string token, string? revokedById = null, string? replacedByToken = null)
    {
        var refreshToken = await GetByTokenAsync(token);

        if (refreshToken == null)
        {
            throw new InvalidOperationException($"Refresh token {token} not found");
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedById = revokedById;
        refreshToken.ReplacedByToken = replacedByToken;

        await UpdateAsync(refreshToken);
    }

    public async Task RevokeAllUserTokensAsync(string userId, string? revokedById = null)
    {
        var tokens = await GetActiveTokensByUserIdAsync(userId);
        
        foreach (var refreshToken in tokens)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedById = revokedById;
        }
        
        await dbContext.SaveChangesAsync();
    }

    public async Task DeletedExpiredTokensAsync()
    {
        // var expiredTokens = await dbContext.RefreshTokens
        //     .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
        //     .ToListAsync();
        //
        // dbContext.RemoveRange(expiredTokens);
        // await dbContext.SaveChangesAsync();

        // NOTE: Are we going to make this shit works faster, ha?
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM RefreshTokens WHERE ExpiresAt < {DateTime.UtcNow} ");
    }
}