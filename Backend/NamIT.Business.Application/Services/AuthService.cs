using Microsoft.EntityFrameworkCore;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Domain.Entities;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public AuthService(IApplicationDbContext db, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<Guid> RegisterTenantAsync(RegisterTenantRequest request)
    {
        var businessType = await _db.BusinessTypes
            .FirstOrDefaultAsync(b => b.Code == request.BusinessTypeCode)
            ?? throw new InvalidOperationException("Loại hình kinh doanh không hợp lệ.");

        var emailExists = await _db.Users.AnyAsync(u => u.Email == request.AdminEmail);
        if (emailExists)
            throw new InvalidOperationException("Email đã được sử dụng.");

        var tenant = new Tenant
        {
            Name = request.TenantName,
            Code = GenerateTenantCode(request.TenantName),
            BusinessTypeId = businessType.Id,
            Status = EntityStatus.Active
        };
        _db.Tenants.Add(tenant);

        // Bật module mặc định theo BusinessType (đơn giản hoá cho Phase 1: POS + PRODUCT + INVENTORY + CUSTOMER + REPORT)
        var defaultModules = new[] { ModuleCode.POS, ModuleCode.PRODUCT, ModuleCode.INVENTORY, ModuleCode.CUSTOMER, ModuleCode.REPORT };
        foreach (var m in defaultModules)
        {
            _db.TenantModules.Add(new TenantModule { TenantId = tenant.Id, ModuleCode = m, IsEnabled = true });
        }

        // Đảm bảo role TENANT_ADMIN tồn tại (role hệ thống dùng chung, TenantId = null)
        var tenantAdminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Code == SystemRole.TENANT_ADMIN.ToString() && r.TenantId == null);
        if (tenantAdminRole == null)
        {
            tenantAdminRole = new Role { Code = SystemRole.TENANT_ADMIN.ToString(), Name = "Quản trị doanh nghiệp", TenantId = null };
            _db.Roles.Add(tenantAdminRole);
        }

        var adminUser = new User
        {
            TenantId = tenant.Id,
            FullName = request.AdminFullName,
            Email = request.AdminEmail,
            PasswordHash = _passwordHasher.Hash(request.AdminPassword),
            Status = EntityStatus.Active
        };
        _db.Users.Add(adminUser);

        _db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = tenantAdminRole.Id });

        // Chi nhánh mặc định đầu tiên
        _db.Branches.Add(new Branch
        {
            TenantId = tenant.Id,
            Name = "Chi nhánh chính",
            Code = "MAIN",
            Status = EntityStatus.Active
        });

        await _db.SaveChangesAsync();
        return tenant.Id;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim();
        if (string.Equals(email, "admin", StringComparison.OrdinalIgnoreCase))
            email = "admin@namit.local";

        var user = await _db.Users
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (user.Status == EntityStatus.Locked)
            throw new UnauthorizedAccessException("Tài khoản đã bị khóa.");

        var roles = user.UserRoles.Select(ur => ur.Role.Code).Distinct().ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct().ToList();

        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(user, user.TenantId, roles, permissions);

        var refreshTokenPlain = _jwtService.GenerateRefreshTokenPlainText();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwtService.HashToken(refreshTokenPlain),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        var profile = new UserProfileDto(user.Id, user.FullName, user.Email, user.TenantId,
            user.Tenant?.Name ?? string.Empty, roles, permissions);

        return new LoginResponse(accessToken, refreshTokenPlain, expiresAt, profile);
    }

    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = _jwtService.HashToken(refreshToken);
        var existing = await _db.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u.Tenant)
            .Include(rt => rt.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (existing == null || !existing.IsActive)
            throw new UnauthorizedAccessException("Refresh token không hợp lệ hoặc đã hết hạn.");

        existing.RevokedAt = DateTime.UtcNow;

        var user = existing.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Code).Distinct().ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct().ToList();

        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(user, user.TenantId, roles, permissions);
        var newRefreshPlain = _jwtService.GenerateRefreshTokenPlainText();
        var newRefreshHash = _jwtService.HashToken(newRefreshPlain);
        existing.ReplacedByTokenHash = newRefreshHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        var profile = new UserProfileDto(user.Id, user.FullName, user.Email, user.TenantId,
            user.Tenant?.Name ?? string.Empty, roles, permissions);

        return new LoginResponse(accessToken, newRefreshPlain, expiresAt, profile);
    }

    public async Task LogoutAsync(Guid userId, string refreshToken)
    {
        var tokenHash = _jwtService.HashToken(refreshToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId && rt.TokenHash == tokenHash);
        if (token != null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private static string GenerateTenantCode(string tenantName)
    {
        var slug = new string(tenantName.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (slug.Length > 8) slug = slug[..8];
        return $"{slug}{Random.Shared.Next(100, 999)}";
    }
}
