using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.Property(r => r.Key);

        builder.Property(r => r.Key).HasMaxLength(200).IsRequired();

        builder.Property(r => r.CreatedAt).IsRequired();

        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => r.Key).IsUnique();

        builder.HasIndex(r => r.CreatedAt);
    }
}