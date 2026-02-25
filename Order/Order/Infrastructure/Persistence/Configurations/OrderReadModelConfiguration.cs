using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrderReadModelConfiguration : IEntityTypeConfiguration<OrderReadModel>
{
    public void Configure(EntityTypeBuilder<OrderReadModel> builder)
    {
        builder.ToTable("OrderReadModels");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.TrackingId).HasMaxLength(100);
        builder.Property(o => o.PaymentId).HasMaxLength(100);
        builder.Property(o => o.Status).IsRequired().HasMaxLength(50);
        builder.Property(o => o.TotalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.Currency).IsRequired().HasMaxLength(3);
        builder.Property(o => o.DeliveryStreet).IsRequired().HasMaxLength(200);
        builder.Property(o => o.DeliveryCity).IsRequired().HasMaxLength(100);
        builder.Property(o => o.DeliveryCountry).IsRequired().HasMaxLength(100);
        builder.Property(o => o.DeliveryPostalCode).IsRequired().HasMaxLength(20);
        
        // store in json for swaggebility
        builder.Property(o => o.ItemsJson).IsRequired().HasColumnType("text");

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt);
        builder.Property(o => o.CompletedAt);
        builder.Property(o => o.Version).IsRequired().IsRequired();
        builder.Property(o => o.LastSyncedAt).IsRequired();
        
        // indexes for common queries
        builder.HasIndex(o => o.CustomerId).HasDatabaseName("OrderReadModels_CustomerId");
        builder.HasIndex(o => o.TrackingId).HasDatabaseName("OrderReadModels_TrackingId");
        builder.HasIndex(o => o.Status).HasDatabaseName("OrderReadModels_Status");
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("OrderReadModels_CreatedAt");
        
        // composite index for pagination queries
        builder.HasIndex(o => new { CreateAt = o.CreatedAt, o.Id }).HasDatabaseName("OrderReadModels_CreatedAt_Id");
    }
}