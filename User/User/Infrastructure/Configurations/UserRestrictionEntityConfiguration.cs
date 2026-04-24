using Domain.Entities.UserRestriction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UserRestrictionEntityConfiguration : IEntityTypeConfiguration<UserRestrictionEntity>
{
    public void Configure(EntityTypeBuilder<UserRestrictionEntity> builder)
    {
        builder.ToTable("UserRestrictions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).IsRequired().HasMaxLength(36);
        builder.Property(r => r.RestrictedUserId).IsRequired().HasMaxLength(36);
        builder.Property(r => r.RestrictedById).IsRequired().HasMaxLength(36);
        builder.Property(r => r.Type).IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(r => r.Reason).IsRequired().HasMaxLength(500);
        builder.Property(r => r.RestrictedAt).IsRequired();
        builder.Property(r => r.ExpiresAt);
        builder.Property(r => r.LiftedAt);
        builder.Property(r => r.LiftedById).HasMaxLength(36);

        builder.HasIndex(r => r.RestrictedUserId);
        builder.HasIndex(r => new { r.RestrictedUserId, r.LiftedAt });

        // Audit records must survive even if actor is deleted
        builder.HasOne(r => r.RestrictedUser)
            .WithMany(u => u.Restrictions)
            .HasForeignKey(r => r.RestrictedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RestrictedBy)
            .WithMany()
            .HasForeignKey(r => r.RestrictedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
