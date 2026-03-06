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
        
        builder.Property(u => u.Id).IsRequired().HasMaxLength(26);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(60);
        builder.Property(u => u.Fullname).HasMaxLength(60);
        builder.Property(u => u.Password).IsRequired().HasMaxLength(40);
        builder.Property(u => u.Phone).IsRequired().HasMaxLength(24);
        builder.Property(u => u.Status).IsRequired().HasDefaultValue(UserStatus.Active);
        builder.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("getutcdate()");
        builder.Property(u => u.UpdatedAt).IsRequired().HasDefaultValueSql("getutcdate()");
    }
}