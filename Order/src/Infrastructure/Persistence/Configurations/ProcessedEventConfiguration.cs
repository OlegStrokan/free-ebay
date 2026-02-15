using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId).IsRequired().ValueGeneratedNever(); // we generate id in domain

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ProcessedBy).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ProcessedAt).IsRequired().HasColumnName("ProcessedAt");
        
        // explicit define for eventId
        builder.HasIndex(e => e.EventId).IsUnique().HasDatabaseName("IX_ProcessedEvents_EventId");

        builder.HasIndex(e => e.ProcessedAt).HasDatabaseName("IX_ProcessedEvents_ProcessedAt");
        builder.HasIndex(e => e.EventType).HasDatabaseName("IX_ProcessedEvents_EventType");
    }
}