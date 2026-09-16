using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NamIT.Business.Application.Interfaces;

namespace NamIT.Business.Infrastructure.Services;

/// <summary>
/// Đọc TenantId/UserId từ claims của JWT đã được xác thực (HttpContext.User),
/// KHÔNG bao giờ đọc từ query string, header tự do, hay body request.
/// Đây là điểm mấu chốt đảm bảo Tenant Isolation ở tầng server.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly Guid _tenantId;
    private readonly Guid? _userId;
    private readonly bool _isSuperAdmin;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        var tenantClaim = user?.FindFirst("tenant_id")?.Value;
        _tenantId = Guid.TryParse(tenantClaim, out var tid) ? tid : Guid.Empty;

        var subClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value;
        _userId = Guid.TryParse(subClaim, out var uid) ? uid : null;

        _isSuperAdmin = user?.IsInRole("SUPER_ADMIN") ?? false;
    }

    public Guid TenantId => _tenantId;
    public Guid? UserId => _userId;
    public bool IsSuperAdmin => _isSuperAdmin;
    public bool HasTenant => _tenantId != Guid.Empty;
}
