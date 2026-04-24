using Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Content).IsRequired().HasColumnType("text");
        builder.Property(x => x.AggregateId).IsRequired().HasMaxLength(255);
        builder.Property(x => x.OccurredOn).IsRequired();
        builder.Property(x => x.ProcessedOn);
        builder.Property(x => x.RetryCount).HasDefaultValue(0);
        builder.Property(x => x.Error).HasMaxLength(2000);

        // Partial index for efficient polling of unprocessed messages
        builder.HasIndex(x => x.ProcessedOn)
            .HasFilter("\"ProcessedOn\" IS NULL");

        builder.HasIndex(x => x.OccurredOn);
    }
}
