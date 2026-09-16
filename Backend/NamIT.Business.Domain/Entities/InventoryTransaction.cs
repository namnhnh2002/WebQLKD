using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Domain.Entities;

public class InventoryTransaction : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = default!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public Guid? ProductVariantId { get; set; }
    public InventoryTransactionType Type { get; set; }
    public int Quantity { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Note { get; set; }

    public Guid? CreatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public enum InventoryTransactionType
{
    PURCHASE = 1,
    SALE = 2,
    RETURN = 3,
    ADJUSTMENT = 4,
    DAMAGE = 5,
    TRANSFER = 6
}
