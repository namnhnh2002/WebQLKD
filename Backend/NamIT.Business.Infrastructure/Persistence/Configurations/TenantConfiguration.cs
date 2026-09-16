using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NamIT.Business.Domain.Entities;

namespace NamIT.Business.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.Name).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Code).HasMaxLength(50).IsRequired();
        builder.HasOne(t => t.BusinessType).WithMany(b => b.Tenants).HasForeignKey(t => t.BusinessTypeId);
    }
}

public class BusinessTypeConfiguration : IEntityTypeConfiguration<BusinessType>
{
    public void Configure(EntityTypeBuilder<BusinessType> builder)
    {
        builder.ToTable("BusinessTypes");
        builder.HasIndex(b => b.Code).IsUnique();
        builder.Property(b => b.Code).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(255).IsRequired();
    }
}

public class TenantModuleConfiguration : IEntityTypeConfiguration<TenantModule>
{
    public void Configure(EntityTypeBuilder<TenantModule> builder)
    {
        builder.ToTable("TenantModules");
        builder.HasIndex(tm => new { tm.TenantId, tm.ModuleCode }).IsUnique();
        builder.HasOne(tm => tm.Tenant).WithMany(t => t.TenantModules).HasForeignKey(tm => tm.TenantId);
    }
}
