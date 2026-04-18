using Domain.Entities.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RoleEntityConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    // Fixed deterministic GUIDs so re-seeding is idempotent
    public static readonly string UserRoleId = "a1b2c3d4-0001-0000-0000-000000000001";
    public static readonly string SellerRoleId = "a1b2c3d4-0001-0000-0000-000000000002";
    public static readonly string ModeratorRoleId  = "a1b2c3d4-0001-0000-0000-000000000003";
    public static readonly string AdminRoleId = "a1b2c3d4-0001-0000-0000-000000000004";
    public static readonly string SuperAdminRoleId = "a1b2c3d4-0001-0000-0000-000000000005";

    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.Name).IsUnique();

        builder.Property(r => r.Id).IsRequired().HasMaxLength(36);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(255);
        builder.Property(r => r.IsSystem).IsRequired().HasDefaultValue(true);

        builder.HasData(
            new RoleEntity { Id = UserRoleId, Name = "User", Description = "Default buyer/browser", IsSystem = true },
            new RoleEntity { Id = SellerRoleId, Name = "Seller", Description = "Can list products", IsSystem = true },
            new RoleEntity { Id = ModeratorRoleId, Name = "Moderator",  Description = "Can block users", IsSystem = true },
            new RoleEntity { Id = AdminRoleId, Name = "Admin", Description = "Full user management", IsSystem = true },
            new RoleEntity { Id = SuperAdminRoleId, Name = "SuperAdmin", Description = "System configuration", IsSystem = true }
        );
    }
}
