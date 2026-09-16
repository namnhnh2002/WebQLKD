using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Domain.Entities;

public class Topping : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class Combo : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ICollection<ComboItem> Items { get; set; } = new List<ComboItem>();
}

public class ComboItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid ComboId { get; set; }
    public Combo Combo { get; set; } = default!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public int Quantity { get; set; }
}

public class KitchenOrder : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public KitchenOrderStatus Status { get; set; } = KitchenOrderStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<KitchenOrderItem> Items { get; set; } = new List<KitchenOrderItem>();
}

public class KitchenOrderItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid KitchenOrderId { get; set; }
    public KitchenOrder KitchenOrder { get; set; } = default!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = default!;
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
