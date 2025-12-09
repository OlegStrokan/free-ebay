using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationTokens");
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).IsRequired().HasMaxLength(26);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(26);
        builder.Property(x => x.Token).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("getutcdate()");
        builder.Property(x => x.IsUsed).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.UsedAt);
        
        // indexes
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.ExpiresAt).IsUnique();
        builder.HasIndex(x => x.UserId).IsUnique();

    }
}