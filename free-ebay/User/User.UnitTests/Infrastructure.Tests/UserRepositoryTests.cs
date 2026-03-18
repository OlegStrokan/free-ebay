using Domain.Entities.User;
using Infrastructure.DbContext;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

public class UserRepositoryTests
{
    private AppDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options;
        return new AppDbContext(options);

    }


    [Fact]
    public async Task CreateUser_ShouldPersistUser()
    {
        var dbContext = GetDbContext("CreateUserDb");
        var userRepository = new UserRepository(dbContext);


        var user = new UserEntity
        {
            Id = "ULID_*#(@*@*(#*U",
            Email = "test@example.com",
            Fullname = "John Doe",
            Password = "password",
            Phone = "+420293829",
            CountryCode = "DE",
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.CreateUser(user);
        var fetchedUser = await userRepository.GetUserById(user.Id);

        Assert.NotNull(fetchedUser);
        Assert.Equal(user.Id, fetchedUser.Id);
        Assert.Equal(user.Email, fetchedUser.Email);
        Assert.Equal(UserStatus.Active, fetchedUser.Status);
        Assert.False(fetchedUser.IsEmailVerified);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnUser()
    {
        var dbContext = GetDbContext("GetUserByIdDb");
        var userRepository = new UserRepository(dbContext);
        
        var user = await userRepository.GetUserById("non_existing_id");
        Assert.Null(user);
    }
    
    [Fact]
    public async Task UpdateUser_ShouldUpdateUser()
    {
        var dbContext = GetDbContext("UpdateUserDb");
        var userRepository = new UserRepository(dbContext);

        var user = new UserEntity
        {
            Id = "ULID_*#(@*@*(#*U",
            Email = "test@example.com",
            Fullname = "John Novak",
            Password = "password",
            Phone = "+380293829",
            CountryCode = "DE",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        
        await userRepository.CreateUser(user);

        const string newEmail = "test@example.com";
        user.Email = newEmail;
        
        await userRepository.UpdateUser(user);
        
        var fetchedUser = await userRepository.GetUserById(user.Id);

        Assert.NotNull(fetchedUser);
        Assert.Equal(newEmail, fetchedUser.Email);

        user.IsEmailVerified = true;
        await userRepository.UpdateUser(user);

        var verifiedUser = await userRepository.GetUserById(user.Id);
        Assert.NotNull(verifiedUser);
        Assert.True(verifiedUser!.IsEmailVerified);
    }

    [Fact]
    public async Task GetUserByEmail_ShouldNormalizeLookup()
    {
        var dbContext = GetDbContext("GetUserByEmailDb");
        var userRepository = new UserRepository(dbContext);

        var user = new UserEntity
        {
            Id = "ULID_*#(@*@*(#*U",
            Email = "test@example.com",
            Fullname = "John Doe",
            Password = "password",
            Phone = "+420293829",
            CountryCode = "DE",
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.CreateUser(user);

        var fetchedUser = await userRepository.GetUserByEmail("  TEST@EXAMPLE.COM  ");

        Assert.NotNull(fetchedUser);
        Assert.Equal(user.Id, fetchedUser!.Id);
    }

    
    [Fact]
    public async Task DeleteUser_ShouldNotThrow_WhenUserDoesNotExist()
    {
        var dbContext = GetDbContext("DeleteUserDb");
        var userRepository = new UserRepository(dbContext);

        await userRepository.DeleteUser("non_existing_id");
    }
}