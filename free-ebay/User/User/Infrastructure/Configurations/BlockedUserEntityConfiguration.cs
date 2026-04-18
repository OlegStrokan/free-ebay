using Domain.Entities.BlockedUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BlockedUserEntityConfiguration : IEntityTypeConfiguration<BlockedUserEntity>
{
    public void Configure(EntityTypeBuilder<BlockedUserEntity> builder)
    {
        builder.ToTable("BlockedUsers");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id).IsRequired().HasMaxLength(36);
        builder.Property(b => b.BlockedUserId).IsRequired().HasMaxLength(36);
        builder.Property(b => b.BlockedById).IsRequired().HasMaxLength(36);
        builder.Property(b => b.Reason).IsRequired().HasMaxLength(200);
        builder.Property(b => b.BlockedAt).IsRequired();

        builder.HasIndex(b => b.BlockedUserId);

        // No cascade on either FK - audit records must survive even if actor is deleted
        builder.HasOne(b => b.BlockedUser)
            .WithMany(u => u.BlockedUserRecords)
            .HasForeignKey(b => b.BlockedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.BlockedBy)
            .WithMany()
            .HasForeignKey(b => b.BlockedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
