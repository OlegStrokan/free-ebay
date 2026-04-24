using Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CompensationRefundRetryConfiguration : IEntityTypeConfiguration<CompensationRefundRetry>
{
    public void Configure(EntityTypeBuilder<CompensationRefundRetry> builder)
    {
        builder.ToTable("CompensationRefundRetries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.PaymentId).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(512);
        builder.Property(x => x.RetryCount).IsRequired();
        builder.Property(x => x.NextAttemptAtUtc).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2048);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.CompletedAtUtc);

        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });

        // Keep only one pending retry row per (OrderId, PaymentId) to avoid retry storms.
        builder.HasIndex(x => new { x.OrderId, x.PaymentId })
            .HasDatabaseName("IX_CompensationRefundRetries_OrderId_PaymentId_Pending")
            .IsUnique()
            .HasFilter("\"Status\" = 0");
    }
}
