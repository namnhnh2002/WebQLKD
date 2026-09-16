using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Domain.Entities;

public class Product : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    public string Code { get; set; } = default!;
    public string? Barcode { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Unit { get; set; } = "pcs";
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int MinStock { get; set; }
    public string? ImageUrl { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public ProductStation Station { get; set; } = ProductStation.None;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
