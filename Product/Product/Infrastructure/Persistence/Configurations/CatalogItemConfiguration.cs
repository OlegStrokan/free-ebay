using System.Text.Json;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("CatalogItems");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CatalogItemId.From(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.CategoryId)
            .HasConversion(id => id.Value, value => CategoryId.From(value))
            .HasColumnName("CategoryId")
            .IsRequired();

        builder.Property(x => x.Gtin)
            .HasColumnName("Gtin")
            .HasMaxLength(32);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        var attributesComparer = new ValueComparer<List<ProductAttribute>>(
            (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null)
                   == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
            c => JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
            c => c.ToList());

        builder.Property<List<ProductAttribute>>("_attributes")
            .HasColumnName("Attributes")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<ProductAttribute>>(v, (JsonSerializerOptions?)null) ?? new List<ProductAttribute>(),
                attributesComparer)
            .IsRequired();

        var imageUrlsComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            c => JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
            c => c.ToList());

        builder.Property<List<string>>("_imageUrls")
            .HasColumnName("ImageUrls")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>(),
                imageUrlsComparer)
            .IsRequired();

        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.Gtin)
            .IsUnique()
            .HasFilter("\"Gtin\" IS NOT NULL");

        builder.Ignore(x => x.Attributes);
        builder.Ignore(x => x.ImageUrls);
    }
}