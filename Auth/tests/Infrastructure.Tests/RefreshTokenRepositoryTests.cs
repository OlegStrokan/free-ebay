using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.DbContext;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class RefreshTokenRepositoryTests
{
    private AppDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistToken()
    {
        var dbContext = GetDbContext("CreateAsync_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var token = new RefreshTokenEntity
        {
            Id = "token_ulid_123",
            UserId = "user_123",
            Token = "refresh_token_abc123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        var result = await repository.CreateAsync(token);

        Assert.NotNull(result);
        Assert.Equal(token.Id, result.Id);
        Assert.Equal(token.UserId, result.UserId);
        Assert.Equal(token.Token, result.Token);
        Assert.False(result.IsRevoked);

        var fetchedToken = await dbContext.RefreshTokens.FindAsync(token.Id);
        Assert.NotNull(fetchedToken);
        Assert.Equal(token.Token, fetchedToken.Token);
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnToken_WhenExists()
    {
        var dbContext = GetDbContext("GetByTokenAsync_Exists_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var token = new RefreshTokenEntity
        {
            Id = "token_ulid_123",
            UserId = "user_123",
            Token = "refresh_token_abc123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await repository.CreateAsync(token);

        var result = await repository.GetByTokenAsync("refresh_token_abc123");

        Assert.NotNull(result);
        Assert.Equal(token.Id, result.Id);
        Assert.Equal(token.UserId, result.UserId);
        Assert.Equal(token.Token, result.Token);
        Assert.False(result.IsRevoked);
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnNull_WhenNotExists()
    {
        var dbContext = GetDbContext("GetByTokenAsync_NotExists_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var result = await repository.GetByTokenAsync("non_existing_token");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnToken_WhenExists()
    {
        var dbContext = GetDbContext("GetByIdAsync_Exists_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var token = new RefreshTokenEntity
        {
            Id = "tokenId",
            UserId = "userId",
            Token = "tokenValue",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await repository.CreateAsync(token);

        var result = await repository.GetByIdAsync("tokenId");

        Assert.NotNull(result);
        Assert.Equal(token.Id, result.Id);
        Assert.Equal(token.Token, result.Token);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        var dbContext = GetDbContext("GetByIdAsync_NotExists_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var result = await repository.GetByIdAsync("nonExistingId");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveTokensByUserIdAsync_ShouldReturnActiveTokens()
    {
        var dbContext = GetDbContext("GetActiveTokens_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var userId = "userId";

        var activeToken1 = new RefreshTokenEntity
        {
            Id = "tokenId1",
            UserId = userId,
            Token = "tokenValue1",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            IsRevoked = false
        };

        // Active token 2 (newer)
        var activeToken2 = new RefreshTokenEntity
        {
            Id = "tokenId2",
            UserId = userId,
            Token = "tokenValue2",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await repository.CreateAsync(activeToken1);
        await repository.CreateAsync(activeToken2);

        // Act
        var result = await repository.GetActiveTokensByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("tokenValue2", result[0].Token); // Newest first
        Assert.Equal("tokenValue1", result[1].Token);
    }

    [Fact]
    public async Task GetActiveTokensByUserIdAsync_ShouldExcludeRevokedTokens()
    {
        // Arrange
        var dbContext = GetDbContext("GetActiveTokens_ExcludeRevoked_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var userId = "userId";

        var activeToken = new RefreshTokenEntity
        {
            Id = "tokenActiveId",
            UserId = userId,
            Token = "tokenActive",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        // Revoked token (should be excluded)
        var revokedToken = new RefreshTokenEntity
        {
            Id = "tokenRevokedId",
            UserId = userId,
            Token = "tokenRevoked",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(activeToken);
        await repository.CreateAsync(revokedToken);
        
        var result = await repository.GetActiveTokensByUserIdAsync(userId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("tokenActive", result[0].Token);
    }

    [Fact]
    public async Task GetActiveTokensByUserIdAsync_ShouldExcludeExpiredTokens()
    {
        var dbContext = GetDbContext("GetActiveTokens_ExcludeExpired_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var userId = "userId";

        var activeToken = new RefreshTokenEntity
        {
            Id = "tokenActiveId",
            UserId = userId,
            Token = "tokenActive",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        var expiredToken = new RefreshTokenEntity
        {
            Id = "tokenExpiredId",
            UserId = userId,
            Token = "tokenExpired",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            IsRevoked = false
        };

        await repository.CreateAsync(activeToken);
        await repository.CreateAsync(expiredToken);

        var result = await repository.GetActiveTokensByUserIdAsync(userId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("tokenActive", result[0].Token);
    }

    [Fact]
    public async Task GetActiveTokensByUserIdAsync_ShouldReturnEmptyList_WhenNoActiveTokens()
    {
        var dbContext = GetDbContext("GetActiveTokens_Empty_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var result = await repository.GetActiveTokensByUserIdAsync("user_123");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateToken()
    {
        var dbContext = GetDbContext("UpdateAsync_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var token = new RefreshTokenEntity
        {
            Id = "tokenId",
            UserId = "userId",
            Token = "tokenValue",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await repository.CreateAsync(token);

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedById = "adminId";
        var result = await repository.UpdateAsync(token);

        Assert.NotNull(result);
        Assert.True(result.IsRevoked);
        Assert.NotNull(result.RevokedAt);
        Assert.Equal("adminId", result.RevokedById);

        var fetchedToken = await repository.GetByTokenAsync(token.Token);
        Assert.NotNull(fetchedToken);
        Assert.True(fetchedToken.IsRevoked);
        Assert.Equal("adminId", fetchedToken.RevokedById);
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldRevokeToken()
    {
        // Arrange
        var dbContext = GetDbContext("RevokeToken_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var token = new RefreshTokenEntity
        {
            Id = "tokenId",
            UserId = "userId",
            Token = "tokenValue",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await repository.CreateAsync(token);

        await repository.RevokeTokenAsync(
            "tokenValue", 
            revokedById: "adminId", 
            replacedByToken: "newTokenValue"
        );

        var revokedToken = await repository.GetByTokenAsync("tokenValue");
        
        Assert.NotNull(revokedToken);
        Assert.True(revokedToken.IsRevoked);
        Assert.NotNull(revokedToken.RevokedAt);
        Assert.Equal("adminId", revokedToken.RevokedById);
        Assert.Equal("newTokenValue", revokedToken.ReplacedByToken);
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldThrow_WhenTokenNotFound()
    {
        var dbContext = GetDbContext("RevokeToken_NotFound_Db");
        var repository = new RefreshTokenRepository(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.RevokeTokenAsync("nonExistingToken")
        );
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeAllActiveTokens()
    {
        var dbContext = GetDbContext("RevokeAllTokens_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var userId = "userId";

        var token1 = new RefreshTokenEntity
        {
            Id = "tokenId1",
            UserId = userId,
            Token = "tokenValue1",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        var token2 = new RefreshTokenEntity
        {
            Id = "tokenId2",
            UserId = userId,
            Token = "tokenValue2",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        var otherUserToken = new RefreshTokenEntity
        {
            Id = "otherTokenId",
            UserId = "userId2",
            Token = "tokenValue3",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await repository.CreateAsync(token1);
        await repository.CreateAsync(token2);
        await repository.CreateAsync(otherUserToken);
        
        await repository.RevokeAllUserTokensAsync(userId, revokedById: "system");

        var revokedToken1 = await repository.GetByTokenAsync("tokenValue1");
        var revokedToken2 = await repository.GetByTokenAsync("tokenValue2");
        var otherToken = await repository.GetByTokenAsync("tokenValue3");

        Assert.NotNull(revokedToken1);
        Assert.True(revokedToken1.IsRevoked);
        Assert.Equal("system", revokedToken1.RevokedById);

        Assert.NotNull(revokedToken2);
        Assert.True(revokedToken2.IsRevoked);
        Assert.Equal("system", revokedToken2.RevokedById);

        Assert.NotNull(otherToken);
        Assert.False(otherToken.IsRevoked);
    }

    [Fact]
    public async Task DeletedExpiredTokensAsync_ShouldDeleteExpiredTokens()
    {
        var dbContext = GetDbContext("DeleteExpired_Db");
        var repository = new RefreshTokenRepository(dbContext);

        var validToken = new RefreshTokenEntity
        {
            Id = "tokenId1",
            UserId = "userId1",
            Token = "validTokenValue",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        // Expired token
        var expiredToken = new RefreshTokenEntity
        {
            Id = "tokenId2",
            UserId = "userId2",
            Token = "expiredTokenValue",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            IsRevoked = false
        };

        await repository.CreateAsync(validToken);
        await repository.CreateAsync(expiredToken);
        
        await repository.DeletedExpiredTokensAsync();

        var remainingTokens = await dbContext.RefreshTokens.ToListAsync();
        Assert.Single(remainingTokens);
        Assert.Equal("validTokenValue", remainingTokens[0].Token);

        var expiredCheck = await repository.GetByTokenAsync("expiredTokenValue");
        Assert.Null(expiredCheck);
    }
}