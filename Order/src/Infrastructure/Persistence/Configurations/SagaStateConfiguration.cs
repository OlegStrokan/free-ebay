using Application.Sagas.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SagaStateConfiguration : IEntityTypeConfiguration<SagaState>
{
    public void Configure(EntityTypeBuilder<SagaState> builder)
    {
        builder.ToTable("SagaStates");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CorrelationId, x.SagaType}).IsUnique();

        builder.Property(x => x.SagaType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CurrentStep).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Payload).HasConversion<string>().HasMaxLength(20);

        builder.Property(x => x.Context).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Payload).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.UpdatedAt);

        builder.HasMany(x => x.Steps)
            .WithOne()
            .HasForeignKey(x => x.SagaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}