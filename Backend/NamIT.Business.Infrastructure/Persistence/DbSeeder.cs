using Microsoft.EntityFrameworkCore;
using NamIT.Business.Domain.Entities;
using NamIT.Business.Domain.Enums;
using NamIT.Business.Infrastructure.Services;

namespace NamIT.Business.Infrastructure.Persistence;

/// <summary>
/// Seed data cơ bản: BusinessTypes, Roles hệ thống, Permissions.
/// Gọi từ Program.cs khi start up (chỉ ở Development, hoặc migration bundle riêng cho Production).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        bool enableDemoAdminFallback = false,
        string? demoAdminPassword = null)
    {
        if (!await db.BusinessTypes.IgnoreQueryFilters().AnyAsync())
        {
            var types = Enum.GetValues<BusinessTypeCode>().Select(code => new BusinessType
            {
                Code = code.ToString(),
                Name = GetBusinessTypeName(code)
            });
            db.BusinessTypes.AddRange(types);
        }

        if (!await db.Roles.IgnoreQueryFilters().AnyAsync())
        {
            var roles = Enum.GetValues<SystemRole>().Select(code => new Role
            {
                Code = code.ToString(),
                Name = code.ToString(),
                TenantId = null
            }).ToList();
            db.Roles.AddRange(roles);
        }

        if (!await db.Permissions.IgnoreQueryFilters().AnyAsync())
        {
            string[] permissionCodes =
            {
                "PRODUCT_VIEW", "PRODUCT_CREATE", "PRODUCT_EDIT", "PRODUCT_DELETE",
                "ORDER_VIEW", "ORDER_CREATE", "ORDER_EDIT", "ORDER_CANCEL",
                "TABLE_VIEW", "TABLE_CREATE", "TABLE_UPDATE", "TABLE_TRANSFER", "TABLE_MERGE", "TABLE_SPLIT",
                "KITCHEN_VIEW", "KITCHEN_UPDATE",
                "INVENTORY_VIEW", "INVENTORY_ADJUST",
                "CUSTOMER_VIEW", "CUSTOMER_CREATE", "CUSTOMER_EDIT",
                "SUPPLIER_VIEW", "SUPPLIER_CREATE", "SUPPLIER_EDIT",
                "PAYMENT_VIEW", "PAYMENT_CREATE", "DEBT_VIEW",
                "REPORT_VIEW",
                "USER_VIEW", "USER_CREATE", "USER_EDIT", "USER_DELETE",
                "SETTINGS_VIEW", "SETTINGS_EDIT"
            };
            db.Permissions.AddRange(permissionCodes.Select(c => new Permission { Code = c, Name = c, GroupName = c.Split('_')[0] }));
        }

        await db.SaveChangesAsync();

        var tenants = await db.Tenants.IgnoreQueryFilters().Include(t => t.BusinessType).ToListAsync();
        foreach (var tenant in tenants)
        {
            var enabled = await db.TenantModules.IgnoreQueryFilters()
                .Where(module => module.TenantId == tenant.Id)
                .Select(module => module.ModuleCode)
                .ToListAsync();
            var defaults = ModuleCatalog.For(Enum.Parse<BusinessTypeCode>(tenant.BusinessType.Code, true));
            db.TenantModules.AddRange(defaults
                .Where(module => !enabled.Contains(module))
                .Select(module => new TenantModule { TenantId = tenant.Id, ModuleCode = module, IsEnabled = true }));
        }
        await db.SaveChangesAsync();

        // Gán toàn bộ permission cho TENANT_ADMIN nếu chưa có
        var tenantAdminRole = await db.Roles.IgnoreQueryFilters().FirstAsync(r => r.Code == SystemRole.TENANT_ADMIN.ToString());
        var hasAnyMap = await db.RolePermissionMaps.IgnoreQueryFilters().AnyAsync(rp => rp.RoleId == tenantAdminRole.Id);
        if (!hasAnyMap)
        {
            var allPermissions = await db.Permissions.IgnoreQueryFilters().ToListAsync();
            foreach (var p in allPermissions)
            {
                db.RolePermissionMaps.Add(new RolePermissionMap { RoleId = tenantAdminRole.Id, PermissionId = p.Id });
            }
            await db.SaveChangesAsync();
        }

        if (enableDemoAdminFallback && !string.IsNullOrWhiteSpace(demoAdminPassword))
            await SeedAdminAsync(db, tenantAdminRole, demoAdminPassword);
    }

    private static async Task SeedAdminAsync(ApplicationDbContext db, Role tenantAdminRole, string demoAdminPassword)
    {
        const string adminEmail = "admin@namit.local";
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == adminEmail))
            return;

        var businessType = await db.BusinessTypes.IgnoreQueryFilters()
            .FirstAsync(b => b.Code == BusinessTypeCode.CAFE.ToString());
        var tenant = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Code == "NAMITADMIN");

        if (tenant == null)
        {
            tenant = new Tenant
            {
                Name = "NamIT Demo Business",
                Code = "NAMITADMIN",
                BusinessTypeId = businessType.Id,
                Status = EntityStatus.Active
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        var admin = new User
        {
            TenantId = tenant.Id,
            FullName = "NamIT Administrator",
            Email = adminEmail,
            PasswordHash = new PasswordHasher().Hash(demoAdminPassword),
            Status = EntityStatus.Active
        };
        db.Users.Add(admin);
        db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = tenantAdminRole.Id });
        db.Branches.Add(new Branch { TenantId = tenant.Id, Name = "Chi nhánh chính", Code = "MAIN", Status = EntityStatus.Active });
        await db.SaveChangesAsync();
    }

    private static string GetBusinessTypeName(BusinessTypeCode code) => code switch
    {
        BusinessTypeCode.CAFE => "Quán cà phê",
        BusinessTypeCode.RESTAURANT => "Nhà hàng",
        BusinessTypeCode.PUB => "Quán nhậu",
        BusinessTypeCode.BILLIARD => "Quán bida",
        BusinessTypeCode.FASHION => "Shop thời trang",
        BusinessTypeCode.RETAIL => "Cửa hàng bán lẻ",
        BusinessTypeCode.GROCERY => "Tạp hóa",
        BusinessTypeCode.MILK_TEA => "Trà sữa",
        BusinessTypeCode.SALON => "Salon",
        _ => "Khác"
    };
}
