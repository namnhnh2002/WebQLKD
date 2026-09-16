using NamIT.Business.Domain.Common;

namespace NamIT.Business.Domain.Entities;

public class DebtTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid? ReferenceId { get; set; }
    public DebtTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedBy { get; set; }
}

public enum DebtTransactionType
{
    SALE_DEBT = 1,
    PAYMENT = 2,
    PURCHASE_DEBT = 3,
    ADJUSTMENT = 4,
    REFUND = 5
}
