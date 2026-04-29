using Application.Models;
using Domain.Entities;
using Infrastructure.Persistence.Configurations;
using Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.DbContext;

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new ProductConfiguration());
        builder.ApplyConfiguration(new CatalogItemConfiguration());
        builder.ApplyConfiguration(new ListingConfiguration());
        builder.ApplyConfiguration(new CategoryConfiguration());
        builder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
