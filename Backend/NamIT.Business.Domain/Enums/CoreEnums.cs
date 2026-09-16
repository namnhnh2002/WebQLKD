namespace NamIT.Business.Domain.Enums;

public enum EntityStatus
{
    Active = 1,
    Inactive = 2,
    Locked = 3
}

/// <summary>
/// Loại hình kinh doanh. Lưu ý: đây chỉ là seed data tham chiếu (bảng BusinessTypes),
/// KHÔNG hard-code logic nghiệp vụ theo enum này ở tầng Core.
/// </summary>
public enum BusinessTypeCode
{
    CAFE,
    RESTAURANT,
    PUB,
    BILLIARD,
    FASHION,
    RETAIL,
    GROCERY,
    MILK_TEA,
    SALON,
    OTHER
}

/// <summary>
/// Danh sách module nghiệp vụ có thể bật/tắt theo Tenant (TenantModules).
/// </summary>
public enum ModuleCode
{
    POS,
    PRODUCT,
    INVENTORY,
    CUSTOMER,
    SUPPLIER,
    TABLE,
    KITCHEN,
    TOPPING,
    BILLIARD_TABLE,
    PRODUCT_VARIANT,
    REPORT,
    EXPENSE,
    APPOINTMENT
}

public enum SystemRole
{
    SUPER_ADMIN,
    TENANT_ADMIN,
    MANAGER,
    SELLER,
    ACCOUNTANT,
    WAREHOUSE,
    CASHIER
}
