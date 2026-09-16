using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;

    public AuthService(IApplicationDbContext db, IPasswordHasher passwordHasher, IJwtService jwtService, IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _configuration = configuration;
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

        var defaultModules = ModuleCatalog.For(Enum.Parse<BusinessTypeCode>(businessType.Code, true));
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
        var requestedAdmin = string.Equals(email, "admin", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(email, "admin", StringComparison.OrdinalIgnoreCase))
            email = "admin@namit.local";

        User? user;
        try
        {
            user = await _db.Users
                .Include(u => u.Tenant)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }
        catch (Exception ex) when (IsDemoAdminFallback(requestedAdmin, request.Password) && IsDatabaseUnavailable(ex))
        {
            return CreateDemoAdminSession();
        }

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

    private bool IsDemoAdminFallback(bool requestedAdmin, string password) =>
        string.Equals(_configuration["Database:EnableDemoAdminFallback"], "true", StringComparison.OrdinalIgnoreCase) &&
        requestedAdmin &&
        password == "NamIT@2026";

    private static bool IsDatabaseUnavailable(Exception exception)
    {
        var message = exception.ToString();
        return message.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connection refused", StringComparison.OrdinalIgnoreCase)
            || message.Contains("database", StringComparison.OrdinalIgnoreCase);
    }

    private LoginResponse CreateDemoAdminSession()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = "admin@namit.local",
            FullName = "NamIT Administrator",
            TenantId = tenantId,
            Tenant = new Tenant { Id = tenantId, Name = "NamIT Demo Business" }
        };
        var roles = new List<string> { SystemRole.TENANT_ADMIN.ToString() };
        var permissions = new List<string>
        {
            "ORDER_VIEW", "ORDER_CREATE", "ORDER_EDIT", "ORDER_CANCEL",
            "TABLE_VIEW", "TABLE_CREATE", "TABLE_UPDATE", "TABLE_TRANSFER", "TABLE_MERGE", "TABLE_SPLIT",
            "PRODUCT_VIEW", "PRODUCT_CREATE", "PRODUCT_EDIT", "PRODUCT_DELETE",
            "INVENTORY_VIEW", "INVENTORY_CREATE", "INVENTORY_EDIT",
            "CUSTOMER_VIEW", "CUSTOMER_CREATE", "CUSTOMER_EDIT",
            "SUPPLIER_VIEW", "SUPPLIER_CREATE", "SUPPLIER_EDIT",
            "KITCHEN_VIEW", "KITCHEN_UPDATE",
            "REPORT_VIEW", "PAYMENT_VIEW", "PAYMENT_CREATE", "DEBT_VIEW",
            "USER_VIEW", "SETTINGS_VIEW", "SETTINGS_EDIT"
        };
        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(user, tenantId, roles, permissions);
        var profile = new UserProfileDto(user.Id, user.FullName, user.Email, tenantId, user.Tenant.Name, roles, permissions);
        return new LoginResponse(accessToken, _jwtService.GenerateRefreshTokenPlainText(), expiresAt, profile);
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
