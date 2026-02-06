using Application.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;


public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Content).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.OccurredOnUtc).IsRequired();
        builder.Property(x => x.ProcessedOnUtc);
        
        builder.HasIndex(x => x.ProcessedOnUtc).HasFilter("[ProcessedOnUtc] IS NULL");

        builder.HasIndex(x => x.OccurredOnUtc);

    }
}
