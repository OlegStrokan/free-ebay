using Domain.Entities.DeliveryInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DeliveryInfoEntityConfiguration : IEntityTypeConfiguration<DeliveryInfo>
{
    public void Configure(EntityTypeBuilder<DeliveryInfo> builder)
    {
        builder.ToTable("DeliveryInfos");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).IsRequired().HasMaxLength(36);
        builder.Property(d => d.UserId).IsRequired().HasMaxLength(36);
        builder.Property(d => d.Street).IsRequired().HasMaxLength(200);
        builder.Property(d => d.City).IsRequired().HasMaxLength(100);
        builder.Property(d => d.PostalCode).IsRequired().HasMaxLength(20);
        builder.Property(d => d.CountryDestination).IsRequired().HasMaxLength(100);
    }
}
