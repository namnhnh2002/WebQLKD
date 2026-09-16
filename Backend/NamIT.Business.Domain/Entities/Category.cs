using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Domain.Entities;

public class Category : BaseEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
