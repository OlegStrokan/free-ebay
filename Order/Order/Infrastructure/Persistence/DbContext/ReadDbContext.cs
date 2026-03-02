using Infrastructure.Persistence.Configurations;
using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.DbContext;

/// <summary>
/// Dedicated read-side DbContext.  Kept separate from AppDbContext so the read database
/// can be pointed at a read replica (or just the same database) without mixing concerns.
/// Connection string: "PostgresReadModel".  Falls back to "Postgres" if not configured.
/// </summary>
public class ReadDbContext(DbContextOptions<ReadDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<OrderReadModel> OrderReadModels => Set<OrderReadModel>();
    public DbSet<ReturnRequestReadModel> ReturnRequestReadModels => Set<ReturnRequestReadModel>();
    public DbSet<B2BOrderReadModel> B2BOrderReadModels => Set<B2BOrderReadModel>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new OrderReadModelConfiguration());
        builder.ApplyConfiguration(new ReturnRequestReadModelConfiguration());
        builder.ApplyConfiguration(new B2BOrderReadModelConfiguration());
    }
}
