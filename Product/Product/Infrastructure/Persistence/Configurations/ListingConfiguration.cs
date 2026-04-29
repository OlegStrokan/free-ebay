using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("Listings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ListingId.From(value))
            .ValueGeneratedNever();

        builder.Property(x => x.CatalogItemId)
            .HasConversion(id => id.Value, value => CatalogItemId.From(value))
            .HasColumnName("CatalogItemId")
            .IsRequired();

        builder.Property(x => x.SellerId)
            .HasConversion(id => id.Value, value => SellerId.From(value))
            .HasColumnName("SellerId")
            .IsRequired();

        builder.ComplexProperty(x => x.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(x => x.StockQuantity)
            .IsRequired();

        builder.Property(x => x.Condition)
            .HasConversion(c => c.Value, value => ListingCondition.FromValue(value))
            .HasColumnName("Condition")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion(s => s.Value, value => ListingStatus.FromValue(value))
            .HasColumnName("Status")
            .IsRequired();

        builder.Property(x => x.SellerNotes)
            .HasColumnName("SellerNotes")
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.CatalogItemId);
        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.CatalogItemId, x.SellerId })
            .IsUnique()
            .HasFilter("\"Status\" <> 4");
    }
}