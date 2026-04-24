using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AggregateSnapshotConfiguration : IEntityTypeConfiguration<AggregateSnapshot>
{
    public void Configure(EntityTypeBuilder<AggregateSnapshot> builder)
    {
        builder.ToTable("AggregateSnapshots");

        builder.HasKey(a => a.Id);

        builder.Property(s => s.AggregateId).HasMaxLength(100).IsRequired();
        builder.Property(s => s.AggregateType).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Version).IsRequired();
        builder.Property(s => s.StateJson).HasColumnType("jsonb").IsRequired();
        builder.Property(s => s.TakenAt);
        builder.HasIndex(s => new { s.AggregateId, s.AggregateType, s.Version })
            .IsUnique()
            .HasDatabaseName("IX_AggregateSnapshots_AggregateType_Version");
    }   
}