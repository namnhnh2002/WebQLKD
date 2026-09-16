using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace NamIT.Business.Api.Controllers;

/// <summary>
/// Endpoint đơn giản để verify JWT + Tenant Isolation hoạt động đúng
/// (dùng cho Login thành công -> load lại profile, và cho việc test tay trên Swagger).
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public MeController(IApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        if (!_tenantContext.UserId.HasValue)
            return Unauthorized(ApiResponse<object>.Fail("Không xác định được người dùng."));

        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == _tenantContext.UserId.Value);

        if (user == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));

        return Ok(ApiResponse<object>.Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            TenantId = _tenantContext.TenantId,
            TenantName = user.Tenant?.Name
        }));
    }

    [HttpGet("context")]
    public async Task<IActionResult> GetContext()
    {
        if (!_tenantContext.UserId.HasValue)
            return Unauthorized(ApiResponse<object>.Fail("Không xác định được người dùng."));

        var user = await _db.Users
            .Include(item => item.Tenant)
            .ThenInclude(tenant => tenant!.BusinessType)
            .Include(item => item.UserRoles).ThenInclude(item => item.Role).ThenInclude(item => item.RolePermissions).ThenInclude(item => item.Permission)
            .FirstOrDefaultAsync(item => item.Id == _tenantContext.UserId.Value);
        if (user?.Tenant == null)
            return NotFound(ApiResponse<object>.Fail("Không tìm thấy tenant hiện tại."));

        var modules = await _db.TenantModules
            .Where(module => module.TenantId == user.TenantId && module.IsEnabled)
            .Select(module => module.ModuleCode.ToString())
            .OrderBy(module => module)
            .ToListAsync();
        var permissions = user.UserRoles.SelectMany(role => role.Role.RolePermissions.Select(map => map.Permission.Code)).Distinct().OrderBy(code => code).ToList();
        return Ok(ApiResponse<TenantModuleContextDto>.Ok(new TenantModuleContextDto(
            user.TenantId,
            user.Tenant.Name,
            user.Tenant.BusinessType.Code,
            modules,
            permissions)));
    }
}
