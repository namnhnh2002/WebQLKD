using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Services;
using NamIT.Business.Api.Authorization;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminOperationsController : ControllerBase
{
    private readonly IAdminOperationsService _service;

    public AdminOperationsController(IAdminOperationsService service)
    {
        _service = service;
    }

    [HttpGet("dashboard/summary")]
    [RequireModule(ModuleCode.REPORT, "REPORT_VIEW")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary() => Ok(await _service.GetDashboardSummaryAsync());

    [HttpGet("reports/summary")]
    [RequireModule(ModuleCode.REPORT)]
    public async Task<ActionResult<ReportSummaryDto>> GetReportSummary() => Ok(await _service.GetReportSummaryAsync());

    [HttpGet("finance/summary")]
    [RequireModule(ModuleCode.PAYMENT, "PAYMENT_VIEW")]
    public async Task<ActionResult<FinanceSummaryDto>> GetFinanceSummary() => Ok(await _service.GetFinanceSummaryAsync());

    [HttpGet("staff")]
    [RequireModule(ModuleCode.REPORT, "USER_VIEW")]
    public async Task<ActionResult<List<StaffDto>>> GetStaff() => Ok(await _service.GetStaffAsync());

    [HttpGet("settings")]
    [RequireModule(ModuleCode.REPORT, "SETTINGS_VIEW")]
    public async Task<ActionResult<TenantSettingsDto>> GetSettings() => Ok(await _service.GetSettingsAsync());
}