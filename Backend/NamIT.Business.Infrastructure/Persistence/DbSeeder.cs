using Microsoft.EntityFrameworkCore;
using NamIT.Business.Domain.Entities;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Infrastructure.Persistence;

/// <summary>
/// Seed data cơ bản: BusinessTypes, Roles hệ thống, Permissions.
/// Gọi từ Program.cs khi start up (chỉ ở Development, hoặc migration bundle riêng cho Production).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
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
                "INVENTORY_VIEW", "INVENTORY_ADJUST",
                "CUSTOMER_VIEW", "CUSTOMER_CREATE", "CUSTOMER_EDIT",
                "REPORT_VIEW",
                "USER_VIEW", "USER_CREATE", "USER_EDIT", "USER_DELETE",
                "SETTINGS_VIEW", "SETTINGS_EDIT"
            };
            db.Permissions.AddRange(permissionCodes.Select(c => new Permission { Code = c, Name = c, GroupName = c.Split('_')[0] }));
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
