using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Domain.Entities;

public class Customer : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GroupId { get; set; }
    public decimal CreditLimit { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
