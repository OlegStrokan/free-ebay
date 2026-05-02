using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RecurringOrderReadModelConfiguration : IEntityTypeConfiguration<RecurringOrderReadModel>
{
    public void Configure(EntityTypeBuilder<RecurringOrderReadModel> builder)
    {
        builder.ToTable("RecurringOrderReadModels");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).IsRequired().ValueGeneratedNever();

        builder.Property(r => r.CustomerId).IsRequired();
        builder.Property(r => r.PaymentMethod).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Frequency).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Status).IsRequired().HasMaxLength(50);

        builder.Property(r => r.NextRunAt).IsRequired();
        builder.Property(r => r.LastRunAt);
        builder.Property(r => r.TotalExecutions).IsRequired();
        builder.Property(r => r.MaxExecutions);

        builder.Property(r => r.DeliveryStreet).IsRequired().HasMaxLength(200);
        builder.Property(r => r.DeliveryCity).IsRequired().HasMaxLength(100);
        builder.Property(r => r.DeliveryCountry).IsRequired().HasMaxLength(100);
        builder.Property(r => r.DeliveryPostalCode).IsRequired().HasMaxLength(20);

        builder.Property(r => r.ItemsJson).IsRequired().HasColumnType("text");

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt);
        builder.Property(r => r.Version).IsRequired();
        builder.Property(r => r.LastSyncedAt).IsRequired();
        builder.Property(r => r.ClaimedAtUtc);

        builder.HasIndex(r => new { r.Status, r.NextRunAt })
            .HasDatabaseName("IX_RecurringOrders_Status_NextRunAt");

        builder.HasIndex(r => r.CustomerId)
            .HasDatabaseName("IX_RecurringOrders_CustomerId");
    }
}
