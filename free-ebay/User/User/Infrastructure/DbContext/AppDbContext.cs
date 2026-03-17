
using Domain.Entities.User;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DbContext;


public class AppDbContext(DbContextOptions<AppDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new UserEntityConfiguration());
        
        base.OnModelCreating(builder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndNormalizationRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditAndNormalizationRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // constrains
    private void ApplyAuditAndNormalizationRules()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<UserEntity>())
        {
            NormalizeUser(entry.Entity);

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    private static void NormalizeUser(UserEntity entity)
    {
        entity.Email = entity.Email.Trim().ToLowerInvariant();
        entity.Fullname = entity.Fullname.Trim();
        entity.Phone = entity.Phone.Trim();
        entity.CountryCode = entity.CountryCode.Trim().ToUpperInvariant();
    }

}
        
        