using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DomainEventConfiguration : IEntityTypeConfiguration<DomainEvent>
{
    public void Configure(EntityTypeBuilder<DomainEvent> builder)
    {
        builder.ToTable("DomainEvents");
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).IsRequired().ValueGeneratedNever();
        builder.Property(e => e.AggregateId).IsRequired().HasMaxLength(100);
        builder.Property(e => e.AggregateType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(200);
        builder.Property(e => e.EventData).IsRequired().HasColumnType("text");
        builder.Property(e => e.Version).IsRequired();
        builder.Property(e => e.OccuredOn).IsRequired().HasColumnName("OccuredOn");
        
        // composite unique index for optimicstic concurrency

        builder.HasIndex(e => new { e.AggregateId, e.AggregateType, e.Version })
            .IsUnique().HasDatabaseName("DomainEvents_Aggregate_Version");
        builder.HasIndex(e => new { e.AggregateId, e.AggregateType })
            .HasDatabaseName("DomainEvents_Aggregate");
        builder.HasIndex(e => e.OccuredOn)
            .HasDatabaseName("DomainEvents_OccurredOn");
        builder.HasIndex(e => e.EventType)
            .HasDatabaseName("DomainEvents_EventType");
    }
}