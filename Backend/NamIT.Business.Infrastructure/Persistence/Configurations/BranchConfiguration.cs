using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NamIT.Business.Domain.Entities;

namespace NamIT.Business.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasIndex(b => new { b.TenantId, b.Code }).IsUnique();
        builder.Property(b => b.Name).HasMaxLength(255).IsRequired();
        builder.Property(b => b.Code).HasMaxLength(50).IsRequired();
        builder.HasOne(b => b.Tenant).WithMany(t => t.Branches).HasForeignKey(b => b.TenantId);
        // Query filter (Tenant + soft-delete) được set tập trung, kết hợp cả 2 điều kiện,
        // trong ApplicationDbContext.OnModelCreating -> tránh nhiều HasQueryFilter ghi đè nhau.
    }
}

public class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.ToTable("UserBranches");
        builder.HasIndex(ub => new { ub.UserId, ub.BranchId }).IsUnique();
        builder.HasOne(ub => ub.User).WithMany(u => u.UserBranches).HasForeignKey(ub => ub.UserId);
        builder.HasOne(ub => ub.Branch).WithMany(b => b.UserBranches).HasForeignKey(ub => ub.BranchId);
    }
}
