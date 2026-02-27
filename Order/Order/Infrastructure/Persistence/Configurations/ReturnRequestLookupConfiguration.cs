using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ReturnRequestLookupConfiguration : IEntityTypeConfiguration<ReturnRequestLookup>
{
    public void Configure(EntityTypeBuilder<ReturnRequestLookup> builder)
    {
        builder.ToTable("ReturnRequestLookups");

        // OrderId is PK: one return request per order (business type shit)
        builder.HasKey(r => r.OrderId);

        builder.Property(r => r.OrderId).IsRequired();
        builder.Property(r => r.ReturnRequestId).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
    }
}
