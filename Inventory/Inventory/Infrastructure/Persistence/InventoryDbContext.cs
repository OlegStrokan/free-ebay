using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

// mixed to one class to not overcomplicate simple things
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<ProductStockEntity> ProductStocks => Set<ProductStockEntity>();

    public DbSet<InventoryReservationEntity> InventoryReservations => Set<InventoryReservationEntity>();

    public DbSet<InventoryReservationItemEntity> InventoryReservationItems => Set<InventoryReservationItemEntity>();

    public DbSet<InventoryMovementEntity> InventoryMovements => Set<InventoryMovementEntity>();

    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductStockEntity>(entity =>
        {
            entity.ToTable("product_stocks", table =>
            {
                table.HasCheckConstraint("ck_product_stocks_available_non_negative", "\"AvailableQuantity\" >= 0");
                table.HasCheckConstraint("ck_product_stocks_reserved_non_negative", "\"ReservedQuantity\" >= 0");
            });
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.ProductId).ValueGeneratedNever();
            entity.Property(x => x.AvailableQuantity).IsRequired();
            entity.Property(x => x.ReservedQuantity).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<InventoryReservationEntity>(entity =>
        {
            entity.ToTable("inventory_reservations");
            entity.HasKey(x => x.ReservationId);
            entity.Property(x => x.ReservationId).ValueGeneratedNever();
            entity.Property(x => x.OrderId).IsRequired();
            entity.Property(x => x.Status).IsRequired().HasMaxLength(32);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => x.OrderId).IsUnique();

            entity.HasMany(x => x.Items)
                .WithOne(x => x.Reservation)
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InventoryReservationItemEntity>(entity =>
        {
            entity.ToTable("inventory_reservation_items");
            entity.HasKey(x => x.ReservationItemId);
            entity.Property(x => x.ReservationItemId).ValueGeneratedNever();
            entity.Property(x => x.ReservationId).IsRequired();
            entity.Property(x => x.ProductId).IsRequired();
            entity.Property(x => x.Quantity).IsRequired();
            entity.HasIndex(x => x.ReservationId);
            entity.HasIndex(x => x.ProductId);
        });

        modelBuilder.Entity<InventoryMovementEntity>(entity =>
        {
            entity.ToTable("inventory_movements");
            entity.HasKey(x => x.MovementId);
            entity.Property(x => x.MovementId).ValueGeneratedNever();
            entity.Property(x => x.ProductId).IsRequired();
            entity.Property(x => x.MovementType).IsRequired().HasMaxLength(32);
            entity.Property(x => x.QuantityDelta).IsRequired();
            entity.Property(x => x.CorrelationId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.ProductId);
            entity.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            entity.ToTable("inventory_outbox_messages");
            entity.HasKey(x => x.OutboxMessageId);
            entity.Property(x => x.OutboxMessageId).ValueGeneratedNever();
            entity.Property(x => x.Topic).IsRequired().HasMaxLength(128);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(128);
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.ProcessedAtUtc);
            entity.Property(x => x.RetryCount).IsRequired();
            entity.Property(x => x.LastError).IsRequired();
            entity.HasIndex(x => x.ProcessedAtUtc);
            entity.HasIndex(x => x.CreatedAtUtc);
        });

        base.OnModelCreating(modelBuilder);
    }
}
