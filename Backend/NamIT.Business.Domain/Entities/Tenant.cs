using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Domain.Entities;

/// <summary>
/// Tenant = một doanh nghiệp/khách hàng thuê nền tảng.
/// Đây là gốc của toàn bộ multi-tenant isolation.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!; // slug duy nhất, vd: SHOP001
    public Guid BusinessTypeId { get; set; }
    public BusinessType BusinessType { get; set; } = default!;
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<TenantModule> TenantModules { get; set; } = new List<TenantModule>();
}

/// <summary>
/// Danh mục loại hình kinh doanh (CAFE, RESTAURANT, BILLIARD, FASHION...).
/// Seed data cố định, quản trị bởi SUPER_ADMIN.
/// </summary>
public class BusinessType : BaseEntity
{
    public string Code { get; set; } = default!; // vd: CAFE
    public string Name { get; set; } = default!; // vd: "Quán cà phê"
    public string? Description { get; set; }
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
}

/// <summary>
/// Module nào đang được bật cho Tenant nào.
/// Quyết định sidebar/menu và các API được phép sử dụng.
/// </summary>
public class TenantModule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public ModuleCode ModuleCode { get; set; }
    public bool IsEnabled { get; set; } = true;
}
