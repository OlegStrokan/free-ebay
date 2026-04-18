using Domain.Entities.DeliveryInfo;
using Domain.Entities.User;
using Domain.Repositories;
using FluentAssertions;
using Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using User.IntegrationTests.Infrastructure;
using Xunit;

namespace User.IntegrationTests.Persistence;

public sealed class UserRepositoryIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public UserRepositoryIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateUser_ShouldPersistNormalizedValues_AndAuditFields()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid().ToString();
        var user = BuildUser(
            id,
            "  TestUser@Example.COM ",
            "  John Doe  ",
            "  +420123456  ",
            " de ");

        await repo.CreateUser(user);

        db.ChangeTracker.Clear();
        var stored = await db.Users.AsNoTracking().SingleAsync(u => u.Id == id);

        stored.Email.Should().Be("testuser@example.com");
        stored.Fullname.Should().Be("John Doe");
        stored.Phone.Should().Be("+420123456");
        stored.CountryCode.Should().Be("DE");
        stored.Status.Should().Be(UserStatus.Active);
        stored.CustomerTier.Should().Be(CustomerTier.Standard);
        stored.IsEmailVerified.Should().BeFalse();
        stored.CreatedAt.Should().NotBe(default);
        stored.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task ExistsByEmail_ShouldNormalizeInput_AndReturnTrue()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var email = $"exists-{Guid.NewGuid():N}@example.com";
        await repo.CreateUser(BuildUser(Guid.NewGuid().ToString(), email));

        var exists = await repo.ExistsByEmail($"  {email.ToUpperInvariant()}  ");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserByEmail_ShouldNormalizeInput_AndReturnUser()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var email = $"get-{Guid.NewGuid():N}@example.com";
        var id = Guid.NewGuid().ToString();
        await repo.CreateUser(BuildUser(id, email));

        var result = await repo.GetUserByEmail($"  {email.ToUpperInvariant()}  ");

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Email.Should().Be(email);
        result.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUser_ShouldKeepCreatedAt_AndRefreshUpdatedAt()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid().ToString();
        var originalCreatedAt = DateTime.UtcNow.AddDays(-7);
        await repo.CreateUser(BuildUser(id, $"update-{Guid.NewGuid():N}@example.com", createdAt: originalCreatedAt));

        var user = await repo.GetUserById(id);
        user.Should().NotBeNull();

        var createdAfterInsert = user!.CreatedAt;
        var updatedAfterInsert = user.UpdatedAt;

        user.Fullname = "  Updated Name  ";
        user.Phone = "  +490000  ";
        user.Email = user.Email.ToUpperInvariant();
        user.CountryCode = " us ";
        user.CreatedAt = DateTime.UtcNow;

        await Task.Delay(25);
        await repo.UpdateUser(user);

        db.ChangeTracker.Clear();
        var updated = await db.Users.AsNoTracking().SingleAsync(u => u.Id == id);

        updated.CreatedAt.Should().Be(createdAfterInsert);
        updated.UpdatedAt.Should().BeAfter(updatedAfterInsert);
        updated.Email.Should().Be(user.Email.Trim().ToLowerInvariant());
        updated.Fullname.Should().Be("Updated Name");
        updated.Phone.Should().Be("+490000");
        updated.CountryCode.Should().Be("US");
    }

    [Fact]
    public async Task UpdateUser_ShouldPersistPassword_AndEmailVerificationFlag()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid().ToString();
        await repo.CreateUser(BuildUser(id, $"verify-{Guid.NewGuid():N}@example.com"));

        var user = await repo.GetUserById(id);
        user.Should().NotBeNull();

        user!.Password = "$2a$12$new-hash";
        user.IsEmailVerified = true;

        await repo.UpdateUser(user);

        db.ChangeTracker.Clear();
        var updated = await db.Users.AsNoTracking().SingleAsync(u => u.Id == id);

        updated.Password.Should().Be("$2a$12$new-hash");
        updated.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUser_ShouldRemoveExistingUser_AndIgnoreMissing()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var id = Guid.NewGuid().ToString();
        await repo.CreateUser(BuildUser(id, $"delete-{Guid.NewGuid():N}@example.com"));

        await repo.DeleteUser(id);
        var deleted = await repo.GetUserById(id);

        deleted.Should().BeNull();

        Func<Task> act = () => repo.DeleteUser(Guid.NewGuid().ToString());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateUser_ShouldThrow_WhenDuplicateEmailAfterNormalization()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        await repo.CreateUser(BuildUser(Guid.NewGuid().ToString(), email));

        Func<Task> act = () => repo.CreateUser(BuildUser(
            Guid.NewGuid().ToString(),
            $"  {email.ToUpperInvariant()}  "));

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GetUserById_ShouldIncludeDeliveryInfos_WhenPresent()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid().ToString();
        var user = BuildUser(id, $"delivery-{Guid.NewGuid():N}@example.com");
        await repo.CreateUser(user);

        var deliveryInfo = new DeliveryInfo
        {
            Id = Guid.NewGuid().ToString(),
            UserId = id,
            Street = "Main St 1",
            City = "Prague",
            PostalCode = "11000",
            CountryDestination = "CZ",
        };

        db.DeliveryInfos.Add(deliveryInfo);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var fetched = await repo.GetUserById(id);

        fetched.Should().NotBeNull();
        fetched!.DeliveryInfos.Should().HaveCount(1);
        fetched.DeliveryInfos[0].Id.Should().Be(deliveryInfo.Id);
        fetched.DeliveryInfos[0].Street.Should().Be("Main St 1");
        fetched.DeliveryInfos[0].City.Should().Be("Prague");
        fetched.DeliveryInfos[0].PostalCode.Should().Be("11000");
        fetched.DeliveryInfos[0].CountryDestination.Should().Be("CZ");
    }

    [Fact]
    public async Task GetUserByEmail_ShouldIncludeDeliveryInfos_WhenPresent()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid().ToString();
        var email = $"delivery-email-{Guid.NewGuid():N}@example.com";
        await repo.CreateUser(BuildUser(id, email));

        db.DeliveryInfos.Add(new DeliveryInfo
        {
            Id = Guid.NewGuid().ToString(),
            UserId = id,
            Street = "Oak Ave 5",
            City = "Berlin",
            PostalCode = "10115",
            CountryDestination = "DE",
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var fetched = await repo.GetUserByEmail(email);

        fetched.Should().NotBeNull();
        fetched!.DeliveryInfos.Should().HaveCount(1);
        fetched.DeliveryInfos[0].Street.Should().Be("Oak Ave 5");
        fetched.DeliveryInfos[0].City.Should().Be("Berlin");
    }

    [Fact]
    public async Task DeleteUser_ShouldCascadeDelete_DeliveryInfos()
    {
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid().ToString();
        await repo.CreateUser(BuildUser(id, $"cascade-{Guid.NewGuid():N}@example.com"));

        var diId = Guid.NewGuid().ToString();
        db.DeliveryInfos.Add(new DeliveryInfo
        {
            Id = diId,
            UserId = id,
            Street = "Pine Rd 9",
            City = "Vienna",
            PostalCode = "1010",
            CountryDestination = "AT",
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await repo.DeleteUser(id);

        var orphan = await db.DeliveryInfos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == diId);
        orphan.Should().BeNull();
    }

    private static UserEntity BuildUser(
        string id,
        string email,
        string fullname = "John Doe",
        string phone = "+420123456",
        string countryCode = "DE",
        DateTime? createdAt = null)
    {
        return new UserEntity
        {
            Id = id,
            Email = email,
            Password = "$2a$12$integration-hash",
            Fullname = fullname,
            Phone = phone,
            CountryCode = countryCode,
            CustomerTier = CustomerTier.Standard,
            Status = UserStatus.Active,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = default,
        };
    }
}
