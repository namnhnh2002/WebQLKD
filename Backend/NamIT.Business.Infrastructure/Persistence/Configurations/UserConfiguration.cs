using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NamIT.Business.Domain.Entities;

namespace NamIT.Business.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(255).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.HasOne(u => u.Tenant).WithMany(t => t.Users).HasForeignKey(u => u.TenantId).IsRequired(false);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasIndex(rt => rt.TokenHash).IsUnique();
        builder.HasOne(rt => rt.User).WithMany(u => u.RefreshTokens).HasForeignKey(rt => rt.UserId);
    }
}
