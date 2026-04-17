using Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);

        builder.HasIndex(u => u.Email)
            .IsUnique();
        
        builder.Property(u => u.Id).IsRequired().HasMaxLength(36);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(254);
        builder.Property(u => u.Fullname).IsRequired().HasMaxLength(120);
        builder.Property(u => u.Password).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Phone).IsRequired().HasMaxLength(32);
        builder.Property(u => u.IsEmailVerified)
            .HasDefaultValue(false)
            .IsRequired();
        
        builder.Property(u => u.CountryCode).IsRequired().HasMaxLength(2);
        builder.Property(u => u.CustomerTier).IsRequired().HasDefaultValue(CustomerTier.Standard);
        builder.Property(u => u.Status).IsRequired().HasDefaultValue(UserStatus.Active);
        builder.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(u => u.UpdatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasMany(u => u.DeliveryInfos)
            .WithOne()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}