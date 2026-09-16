using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Domain.Entities;

/// <summary>
/// Role hệ thống: SUPER_ADMIN, TENANT_ADMIN, MANAGER, SELLER, ACCOUNTANT, WAREHOUSE, CASHIER.
/// Role có thể là Global (dùng chung mọi tenant, TenantId = null) hoặc theo Tenant nếu sau này cần custom role.
/// </summary>
public class Role : BaseEntity
{
    public Guid? TenantId { get; set; } // null = role hệ thống dùng chung
    public string Code { get; set; } = default!; // vd: TENANT_ADMIN
    public string Name { get; set; } = default!;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermissionMap> RolePermissions { get; set; } = new List<RolePermissionMap>();
}

/// <summary>
/// Permission chi tiết: PRODUCT_VIEW, PRODUCT_CREATE, ORDER_CANCEL, ...
/// </summary>
public class Permission : BaseEntity
{
    public string Code { get; set; } = default!; // vd: PRODUCT_VIEW
    public string Name { get; set; } = default!;
    public string? GroupName { get; set; } // vd: "Product", "Order"...

    public ICollection<RolePermissionMap> RolePermissions { get; set; } = new List<RolePermissionMap>();
}

public class RolePermissionMap : BaseEntity
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;
}
