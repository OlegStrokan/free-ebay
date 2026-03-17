using Domain.Entities;
using Domain.Repositories;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext dbContext) : IRefreshTokenRepository
{
    public async Task<RefreshTokenEntity?> GetByTokenAsync(string refreshToken)
    {
        return await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
    }

    public async Task<RefreshTokenEntity?> GetByIdAsync(string id)
    {
        return await dbContext.RefreshTokens.FindAsync(id);
    }

    public Task<List<RefreshTokenEntity>> GetActiveTokensByUserIdAsync(string userId)
    {
       return dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked &&  rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }

    public async Task<RefreshTokenEntity> CreateAsync(RefreshTokenEntity refreshTokenEntity)
    {
        dbContext.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync();
        return refreshTokenEntity;
    }

    public async Task<RefreshTokenEntity> UpdateAsync(RefreshTokenEntity refreshTokenEntity)
    {
        dbContext.Update(refreshTokenEntity);
        await dbContext.SaveChangesAsync();
        return refreshTokenEntity;
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
        //  Use fucking FE Core instead of raw SQL for InMemory database compatibility
        var expiredTokens = await dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        dbContext.RemoveRange(expiredTokens);
        await dbContext.SaveChangesAsync();

        // Note: If you need raw SQL for production performance with real SQL Server:
        // await dbContext.Database.ExecuteSqlInterpolatedAsync(
        //     $"DELETE FROM RefreshTokens WHERE ExpiresAt < {DateTime.UtcNow}");
    }
}