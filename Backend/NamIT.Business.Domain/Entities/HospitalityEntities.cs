using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Domain.Entities;

public class TableArea : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ICollection<DiningTable> Tables { get; set; } = new List<DiningTable>();
}

public class DiningTable : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid TableAreaId { get; set; }
    public TableArea TableArea { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public DiningTableStatus Status { get; set; } = DiningTableStatus.Available;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ICollection<TableOrder> TableOrders { get; set; } = new List<TableOrder>();
}

public class TableOrder : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid TableId { get; set; }
    public DiningTable Table { get; set; } = default!;
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public string? ExternalOrderNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
}