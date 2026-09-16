using Microsoft.EntityFrameworkCore;
using NamIT.Business.Domain.Entities;

namespace NamIT.Business.Application.Interfaces;

/// <summary>
/// Trừu tượng hóa DbContext để tầng Application không phụ thuộc trực tiếp vào
/// implementation của Infrastructure (ApplicationDbContext implement interface này).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<BusinessType> BusinessTypes { get; }
    DbSet<TenantModule> TenantModules { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<InventoryTransaction> InventoryTransactions { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderItemTopping> OrderItemToppings { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Debt> Debts { get; }
    DbSet<DebtTransaction> DebtTransactions { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<TableArea> TableAreas { get; }
    DbSet<DiningTable> Tables { get; }
    DbSet<TableOrder> TableOrders { get; }
    DbSet<Topping> Toppings { get; }
    DbSet<Combo> Combos { get; }
    DbSet<ComboItem> ComboItems { get; }
    DbSet<KitchenOrder> KitchenOrders { get; }
    DbSet<KitchenOrderItem> KitchenOrderItems { get; }
    DbSet<User> Users { get; }
    DbSet<UserBranch> UserBranches { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermissionMap> RolePermissionMaps { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}
