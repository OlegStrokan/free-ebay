using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        builder.ToTable("DeadLetterMessages");
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Id).IsRequired().ValueGeneratedNever();
        
        builder.Property(d => d.Type).IsRequired().HasMaxLength(255);
        builder.Property(d => d.Content).IsRequired().HasColumnType("text");
        builder.Property(d => d.OccuredOn).IsRequired();
        builder.Property(d => d.FailureReason).IsRequired().HasColumnName("text");
        builder.Property(d => d.RetryCount);
        builder.Property(d => d.MovedToDeadLetterAt).IsRequired();
        builder.Property(d => d.DeadLetterRetryCount).IsRequired();
        builder.Property(d => d.LastRetryAttempt);
        builder.Property(d => d.IsResolved).IsRequired();
        builder.Property(d => d.ResolvedAt);
        builder.Property(d => d.ResolutionNotes).HasMaxLength(1000);

        builder.HasIndex(d => d.Type)
            .HasDatabaseName("IX_DeadLetterMessages_Type");

        builder.HasIndex(d => d.MovedToDeadLetterAt)
            .HasDatabaseName("IX_DeadLetterMessages_IsResolved");

        builder.HasIndex(d => d.IsResolved)
            .HasDatabaseName("IX_DeadLetterMessages_IsResolved");

        builder.HasIndex(d => new { d.IsResolved, d.MovedToDeadLetterAt })
            .HasDatabaseName("IX_DeadLetterMessage_IsResolved_MovedToDeadLetterAt");
    }
}