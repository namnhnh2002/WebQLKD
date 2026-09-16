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
    POS = 0,
    PRODUCT = 1,
    INVENTORY = 2,
    CUSTOMER = 3,
    SUPPLIER = 4,
    TABLE = 5,
    KITCHEN = 6,
    TOPPING = 7,
    BILLIARD_TABLE = 8,
    PRODUCT_VARIANT = 9,
    REPORT = 10,
    EXPENSE = 11,
    APPOINTMENT = 12,
    PAYMENT = 13,
    DEBT = 14,
    COMBO = 15,
    BILLIARD = 16,
    TIME_PRICING = 17,
    SIZE = 18,
    COLOR = 19,
    BARCODE = 20
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

public enum OrderStatus
{
    Draft = 1,
    Completed = 2,
    Cancelled = 3
}

public enum PaymentMethod
{
    CASH = 1,
    CARD = 2,
    TRANSFER = 3,
    E_WALLET = 4,
    DEBT = 5
}

public enum DebtStatus
{
    Open = 1,
    PartiallyPaid = 2,
    Paid = 3
}

public enum DiningTableStatus
{
    Available = 1,
    Occupied = 2,
    Reserved = 3,
    Cleaning = 4,
    Disabled = 5
}

public enum KitchenOrderStatus
{
    Pending = 1,
    Preparing = 2,
    Ready = 3,
    Served = 4,
    Cancelled = 5
}

public enum ProductStation
{
    None = 0,
    Kitchen = 1,
    Bar = 2
}
