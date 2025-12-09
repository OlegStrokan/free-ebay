using Domain.Entities;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DbContext;

public class AppDbContext(DbContextOptions<AppDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
        builder.ApplyConfiguration(new PasswordResetTokenConfiguration());
        builder.ApplyConfiguration(new EmailVerificationTokenConfiguration());
    }

}