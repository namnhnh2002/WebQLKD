using NamIT.Business.Domain.Common;

namespace NamIT.Business.Domain.Entities;

/// <summary>
/// Ghi lại các thao tác quan trọng. KHÔNG lưu password/token trong log.
/// </summary>
public class AuditLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = default!;   // vd: "PRODUCT_CREATE", "LOGIN"
    public string? EntityName { get; set; }           // vd: "Product"
    public string? EntityId { get; set; }
    public string? Detail { get; set; }                // JSON, KHÔNG chứa secret
    public string? IpAddress { get; set; }
}
