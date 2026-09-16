namespace NamIT.Business.Domain.Common;

/// <summary>
/// Base class cho mọi entity. Mọi bảng đều có Id, CreatedAt, UpdatedAt.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Dùng cho các entity thuộc về một Tenant cụ thể.
/// Mọi entity nghiệp vụ (Product, Order, Customer...) phải implement interface này
/// để tầng Infrastructure có thể tự động áp dụng Global Query Filter theo Tenant.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}

/// <summary>
/// Dùng cho các entity hỗ trợ soft delete (KHÔNG hard delete Orders, Payments,
/// InventoryTransactions, DebtTransactions...).
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Dùng cho các entity cần audit ai tạo/sửa.
/// </summary>
public interface IAuditable
{
    Guid? CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
}
