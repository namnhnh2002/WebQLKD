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
    DbSet<User> Users { get; }
    DbSet<UserBranch> UserBranches { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermissionMap> RolePermissionMaps { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
