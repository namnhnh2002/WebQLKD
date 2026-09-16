using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Domain.Common;
using NamIT.Business.Domain.Entities;

namespace NamIT.Business.Infrastructure.Persistence;

/// <summary>
/// DbContext trung tâm. Áp dụng Global Query Filter theo TenantId cho MỌI entity
/// implement ITenantEntity, dựa trên ITenantContext lấy từ JWT (server-side),
/// KHÔNG dựa vào bất kỳ giá trị nào frontend tự gửi lên.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<BusinessType> BusinessTypes => Set<BusinessType>();
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemTopping> OrderItemToppings => Set<OrderItemTopping>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<DebtTransaction> DebtTransactions => Set<DebtTransaction>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<TableArea> TableAreas => Set<TableArea>();
    public DbSet<DiningTable> Tables => Set<DiningTable>();
    public DbSet<TableOrder> TableOrders => Set<TableOrder>();
    public DbSet<Topping> Toppings => Set<Topping>();
    public DbSet<Combo> Combos => Set<Combo>();
    public DbSet<ComboItem> ComboItems => Set<ComboItem>();
    public DbSet<KitchenOrder> KitchenOrders => Set<KitchenOrder>();
    public DbSet<KitchenOrderItem> KitchenOrderItems => Set<KitchenOrderItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermissionMap> RolePermissionMaps => Set<RolePermissionMap>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.Entity<TableOrder>()
            .HasIndex(item => item.TableId)
            .HasFilter("\"IsActive\" = TRUE")
            .IsUnique();

        // Áp dụng Global Query Filter tự động cho mọi entity implement ITenantEntity
        // (và kết hợp thêm điều kiện !IsDeleted nếu entity cũng implement ISoftDeletable).
        // Filter luôn dựa vào ITenantContext (server-side, lấy từ JWT) - KHÔNG bao giờ tin
        // giá trị TenantId do client tự gửi lên.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType)) continue;

            var method = typeof(ApplicationDbContext)
                .GetMethod(nameof(BuildTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);
            var filter = method.Invoke(this, null)!;
            entityType.SetQueryFilter((LambdaExpression)filter);
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Build biểu thức lọc: e => e.TenantId == currentTenantId [&& !e.IsDeleted nếu có].
    /// </summary>
    private LambdaExpression BuildTenantFilter<TEntity>() where TEntity : class, ITenantEntity
    {
        Expression<Func<Guid>> tenantIdAccessor = () => _tenantContext.TenantId;
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
        Expression body = Expression.Equal(tenantIdProperty, tenantIdAccessor.Body);

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
        {
            var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var notDeleted = Expression.Not(isDeletedProperty);
            body = Expression.AndAlso(body, notDeleted);
        }

        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Tự động gán TenantId khi tạo mới entity thuộc Tenant, lấy từ server-side context.
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is ITenantEntity tenantEntity && entry.State == EntityState.Added)
            {
                if (tenantEntity.TenantId == Guid.Empty && _tenantContext.HasTenant)
                {
                    tenantEntity.TenantId = _tenantContext.TenantId;
                }
            }

            if (entry.Entity is BaseEntity baseEntity && entry.State == EntityState.Modified)
            {
                baseEntity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (!Database.IsRelational())
            return await operation();

        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
