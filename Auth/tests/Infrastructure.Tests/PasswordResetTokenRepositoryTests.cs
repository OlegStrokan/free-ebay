using System.Security.Cryptography.X509Certificates;
using Domain.Entities;
using Infrastructure.DbContext;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

public class PasswordResetTokenRepositoryTests
{
    public AppDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistToken()
    {
        var dbContext = GetDbContext("CreateAsync_Db");
        var passwordResetTokenRepository = new PasswordResetTokenRepository(dbContext);

        var token = new PasswordResetTokenEntity
        {
            Id = "tokenId",
            Token = "token",
            UserId = "userId",
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        
        var result = await  passwordResetTokenRepository.CreateAsync(token);

        Assert.NotNull(result);
        Assert.Equal(token.Id, result.Id);
        Assert.Equal(token.Token, result.Token);
        Assert.Equal(token.UserId, result.UserId);
        Assert.Equal(token.IsUsed, result.IsUsed);
        Assert.Equal(token.CreatedAt, result.CreatedAt);
        Assert.Equal(token.ExpiresAt, result.ExpiresAt);

        var fetchedToken = await passwordResetTokenRepository.GetByTokenAsync(token.Token);
        
        Assert.NotNull(fetchedToken);
        Assert.Equal(token.Token, fetchedToken.Token);
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnToken_WhenExists()
    {
        var dbContext = GetDbContext("GetByTokenAsync_Exists_Db");
        var repository = new PasswordResetTokenRepository(dbContext);

        var token = new PasswordResetTokenEntity
        {
            Id = "tokenId",
            Token = "token",
            UserId = "userId",
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        await repository.CreateAsync(token);

        var result = await repository.GetByTokenAsync(token.Token);

        Assert.NotNull(result);
        Assert.Equal(token.Token, result.Token);
        Assert.Equal(token.UserId, result.UserId);
        Assert.Equal(token.Id, result.Id);
        Assert.False(result.IsUsed);
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnNull_WhenNotExists()
    {
        var dbContext = GetDbContext("GetByTokenAsync_NotExists_Db");
        var repository = new PasswordResetTokenRepository(dbContext);
        
        var result = await repository.GetByTokenAsync("nonExistingToken");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnValidUnusedTokens()
    {
        var dbContext = GetDbContext("GetByUserIdAsync_Valid_Db");
        var repository = new PasswordResetTokenRepository(dbContext);

        var userId = "userId";

        var oldToken = new PasswordResetTokenEntity()
        {
            Id = "tokenId",
            Token = "token",
            UserId = "userId",
            IsUsed = false,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        
        var newestToken = new PasswordResetTokenEntity()
        {
            Id = "newTokenId",
            UserId = "userId",
            Token = "newToken",
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };


        await repository.CreateAsync(oldToken);
        await repository.CreateAsync(newestToken);

        var result = await repository.GetByUserIdAsync(userId);
        
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(newestToken.Token, result[0].Token);
        Assert.Equal(oldToken.Token, result[1].Token);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmptyList_WhenAllTokensUsed()
    {
        var dbContext = GetDbContext("GetByUserIdAsync_AllUsed_Db");
        var repository = new PasswordResetTokenRepository(dbContext);
        
        const string userId = "userId";
        
        var usedToken = new PasswordResetTokenEntity
        {
            Id = "token_used",
            UserId = userId,
            Token = "used_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = true,
            UsedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(usedToken);

        var result = await repository.GetByUserIdAsync(userId);
        
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmptyList_WhenAllTokensExpired()
    {
        var dbContext = GetDbContext("GetByUserIdAsync_AllUsed_Db");
        var repository = new PasswordResetTokenRepository(dbContext);

        var userId = "userId";
        
        var expiredToken = new PasswordResetTokenEntity
        {
            Id = "expiredTokenId",
            UserId = userId,
            Token = "expiredToken",
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
        };
        
        await repository.CreateAsync(expiredToken);

        var result = await repository.GetByUserIdAsync(userId);
        
        Assert.NotNull(result);
        Assert.Empty(result);
        

    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmptyList_WhenUserHasNoTokens()
    {
        var dbContext = GetDbContext("GetByUserIdAsync_NoTokens_Db");
        var repository = new PasswordResetTokenRepository(dbContext);

        var result = await repository.GetByUserIdAsync("userId");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldFilterCorrectly()
    {
        var dbContext = GetDbContext("GetByUserIdAsync_FilterCorrectly_Db");
        var repository = new PasswordResetTokenRepository(dbContext);
        
        var userId = "userId";

        var usedToken = new PasswordResetTokenEntity
        {
            Id = "usedTokenId",
            UserId = userId,
            Token = "usedToken",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = true,
        };

        var expiredToken = new PasswordResetTokenEntity
        {
            Id = "expiredTokenId",
            UserId = userId,
            Token = "expiredToken",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
        };

        var token = new PasswordResetTokenEntity()
        {
            Id = "tokenId",
            UserId = userId,
            Token = "expiredToken",
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
        };
        
        await repository.CreateAsync(expiredToken);
        await repository.CreateAsync(usedToken);
        await repository.CreateAsync(token);
        
        var result = await repository.GetByUserIdAsync(userId);
        
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(expiredToken.Token, result.First().Token);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateToken()
    {
        var dbContext = GetDbContext("UpdateAsync_Db");
        var repository = new PasswordResetTokenRepository(dbContext);

        var token = new PasswordResetTokenEntity
        {
            Id = "tokenId",
            UserId = "userId",
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = true,
        };

        await repository.CreateAsync(token);

        token.IsUsed = true;
        token.UsedAt = DateTime.UtcNow;

        var result = await repository.UpdateAsync(token);

        Assert.NotNull(result);
        Assert.True(result.IsUsed);
        Assert.NotNull(result.UsedAt);

        var fetchedToken = await repository.GetByTokenAsync(token.Token);
        Assert.NotNull(fetchedToken);
        Assert.True(result.IsUsed);
        Assert.NotNull(fetchedToken.UsedAt);
    }

    [Fact]
    public async Task MarkAsUsedAsync_ShouldMarkTokenAsUsed()
    {
        var dbContext = GetDbContext("MarkAsUsedAsync_Db");
        var repository = new PasswordResetTokenRepository(dbContext);

        var token = new PasswordResetTokenEntity
        {
            Id = "tokenId",
            UserId = "userId",
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = true,
        };
        
        await repository.CreateAsync(token);

        await repository.MarkAsUsedAsync(token.Token);

        var updatedToken = await repository.GetByTokenAsync(token.Token);

        Assert.NotNull(updatedToken);
        Assert.True(updatedToken.IsUsed);
        Assert.NotNull(updatedToken.UsedAt);
    }

    [Fact]
    public async Task MarkAsUsedAsync_ShouldMarkTokenAsNotUsed()
    {
        var dbContext = GetDbContext("MarkAsUsedAsync_Db");
        var repository = new PasswordResetTokenRepository(dbContext);
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.MarkAsUsedAsync("token"));
    }

    [Fact]
    public async Task DeleteExpiredTokenAsync_ShouldDeleteExpiredToken()
    {
        var dbContext = GetDbContext("DeleteExpiredTokenAsync_All_Db");
        var repository = new PasswordResetTokenRepository(dbContext);

        var validToken = new PasswordResetTokenEntity {
            Id = "validTokenId",
            UserId = "userId1",
            Token = "validToken",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
        };

        var expiredToken = new PasswordResetTokenEntity
        {
            Id = "expiredTokenId",
            UserId = "userId2",
            Token = "expiredToken",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            IsUsed = false,
        };

        var usedToken = new PasswordResetTokenEntity
        {
            Id = "usedTokenId",
            UserId = "userId3",
            Token = "usedToken",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            IsUsed = true,
            UsedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(validToken);
        await repository.CreateAsync(expiredToken);
        await repository.CreateAsync(usedToken);

        await repository.DeleteExpiredTokensAsync();

        var remainingTokens = await dbContext.PasswordResetTokens.ToListAsync(); 
        
        Assert.Single(remainingTokens);
        Assert.Equal(validToken.Token, remainingTokens.First().Token);
        
        
        // verify if expired and used token are deleted
        var expiredCheck = await repository.GetByTokenAsync(expiredToken.Token);
        var usedCheck = await repository.GetByTokenAsync(usedToken.Token);
        
        Assert.Null(expiredCheck);
        Assert.Null(usedCheck);
    }

    [Fact]
    public async Task DeleteExpiredTokensAsync_ShouldDeleteAllUsersExpiredTokens()
    {
        var dbContext = GetDbContext("DeleteExpiredTokenAsync_User_Db");
        var repository = new PasswordResetTokenRepository(dbContext);

        var userId = "usedId";
        
        
        var userToken = new PasswordResetTokenEntity
        {
            Id = "validTokenId",
            UserId = userId,
            Token = "validToken",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
        };

        var userTokenExpired = new PasswordResetTokenEntity
        {
            Id = "expiredTokenId",
            UserId = userId,
            Token = "expiredToken",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = true,
            UsedAt = DateTime.UtcNow
        };

        var anotherUserToken = new PasswordResetTokenEntity
        {
            Id = "usedTokenId",
            UserId = "userId2",
            Token = "usedToken",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
        };
        
        await repository.CreateAsync(userTokenExpired);
        await repository.CreateAsync(userToken);
        await repository.CreateAsync(anotherUserToken);

        await repository.DeleteByUserIdAsync(userId);
        
        var remainingTokens = await dbContext.PasswordResetTokens.ToListAsync();
        
        Assert.Single(remainingTokens);
        Assert.Equal(anotherUserToken.UserId, remainingTokens.First().UserId);
        
        
        var deletedToken1 = await repository.GetByTokenAsync(userTokenExpired.Token);
        var deletedToken2 = await repository.GetByTokenAsync(userTokenExpired.Token);
        
        Assert.Null(deletedToken1);
        Assert.Null(deletedToken2);
        
    }
    
}