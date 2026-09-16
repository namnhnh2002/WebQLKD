using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Domain.Entities;

public class Order : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = default!;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string OrderNumber { get; set; } = default!;
    public OrderStatus Status { get; set; } = OrderStatus.Completed;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DebtAmount { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Debt> Debts { get; set; } = new List<Debt>();
    public Receipt? Receipt { get; set; }
}

public class OrderItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }
    public ICollection<OrderItemTopping> Toppings { get; set; } = new List<OrderItemTopping>();
}

public class OrderItemTopping : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = default!;
    public Guid ToppingId { get; set; }
    public Topping Topping { get; set; } = default!;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}

public class Payment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}

public class Debt : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public DebtStatus Status { get; set; } = DebtStatus.Open;
}

public class Receipt : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public string ReceiptNumber { get; set; } = default!;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
