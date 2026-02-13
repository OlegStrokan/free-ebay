using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReturnRequestReadModelConfiguration : IEntityTypeConfiguration<ReturnRequestReadModel>
{
    public void Configure(EntityTypeBuilder<ReturnRequestReadModel> builder)
    {
        builder.ToTable("ReturnRequestReadModels");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).IsRequired().ValueGeneratedNever();
        builder.Property(r => r.OrderId).IsRequired();
        builder.Property(r => r.CustomerId).IsRequired();
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.Reason).IsRequired().HasMaxLength(50);
        builder.Property(r => r.RefundAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(r => r.Currency).IsRequired().HasMaxLength(3);
        builder.Property(r => r.ItemsToReturnJson).IsRequired().HasMaxLength(3);
        builder.Property(r => r.ItemsToReturnJson).IsRequired().HasColumnType("text");
        builder.Property(r => r.RequestedAt).IsRequired();
        builder.Property(r => r.UpdatedAt);
        builder.Property(r => r.CompletedAt);
        builder.Property(r => r.Version).IsRequired();
        builder.Property(r => r.LastSyncedAt).IsRequired();

        builder.HasIndex(r => r.OrderId)
            .HasDatabaseName("IX_ReturnRequestReadModels_OrderId");
        builder.HasIndex(r => r.CustomerId)
            .HasDatabaseName("IX_ReturnRequestReadModel_CustomerId");
        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_ReturnRequestReadModels_Status");
        builder.HasIndex(r => r.RequestedAt)
            .HasDatabaseName("IX_ReturnRequestReadModels_RequestedAt");
        builder.HasIndex(r => new { r.RequestedAt, r.Id })
            .HasDatabaseName("IX_ReturnRequestReadModels_RequestedAt_Id");
    }
}