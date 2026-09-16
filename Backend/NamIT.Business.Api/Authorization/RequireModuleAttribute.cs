using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireModuleAttribute : TypeFilterAttribute
{
    public RequireModuleAttribute(ModuleCode module, string? permission = null)
        : base(typeof(RequireModuleFilter))
    {
        Arguments = new object[] { module, permission ?? string.Empty };
    }
}

public sealed class RequireModuleFilter : IAsyncActionFilter
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ModuleCode _module;
    private readonly string _permission;

    public RequireModuleFilter(IApplicationDbContext db, ITenantContext tenantContext, ModuleCode module, string? permission)
    {
        _db = db;
        _tenantContext = tenantContext;
        _module = module;
        _permission = permission ?? string.Empty;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_tenantContext.HasTenant)
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.Fail("Không xác định được tenant."));
            return;
        }

        var enabled = await _db.TenantModules.AnyAsync(module => module.TenantId == _tenantContext.TenantId && module.ModuleCode == _module && module.IsEnabled);
        if (!enabled)
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail($"Module {_module} chưa được bật cho tenant này.")) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        if (!string.IsNullOrWhiteSpace(_permission) && !_tenantContext.IsSuperAdmin && !context.HttpContext.User.IsInRole(SystemRole.TENANT_ADMIN.ToString()) && !context.HttpContext.User.Claims.Any(claim => claim.Type == "permission" && claim.Value == _permission))
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("Bạn không có quyền thực hiện thao tác này.")) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }
}