using Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class KafkaRetryRecordConfiguration : IEntityTypeConfiguration<KafkaRetryRecord>
{
    public void Configure(EntityTypeBuilder<KafkaRetryRecord> builder)
    {
        builder.ToTable("KafkaRetryRecords");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).IsRequired().ValueGeneratedNever();

        builder.Property(r => r.EventId);
        builder.Property(r => r.EventType).IsRequired().HasMaxLength(255);
        builder.Property(r => r.Topic).IsRequired().HasMaxLength(255);
        builder.Property(r => r.Partition).IsRequired();
        builder.Property(r => r.Offset).IsRequired();
        builder.Property(r => r.MessageKey).HasMaxLength(255);
        builder.Property(r => r.Payload).IsRequired().HasColumnType("text");
        builder.Property(r => r.Headers).HasColumnType("text");
        builder.Property(r => r.FirstFailureTime).IsRequired();
        builder.Property(r => r.LastFailureTime).IsRequired();
        builder.Property(r => r.RetryCount).IsRequired();
        builder.Property(r => r.NextRetryAt).IsRequired();
        builder.Property(r => r.Status).IsRequired().HasConversion<int>();
        builder.Property(r => r.LastErrorMessage).HasColumnType("text");
        builder.Property(r => r.LastErrorType).HasMaxLength(512);
        builder.Property(r => r.CorrelationId).HasMaxLength(512);

        builder.HasIndex(r => new { r.Status, r.NextRetryAt })
            .HasDatabaseName("IX_KafkaRetryRecords_Status_NextRetryAt")
            .HasFilter("\"Status\" = 0");

        builder.HasIndex(r => r.EventId)
            .HasDatabaseName("IX_KafkaRetryRecords_EventId");
    }
}
