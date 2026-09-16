namespace NamIT.Business.Application.DTOs;

public record ReportSummaryDto(
    decimal Revenue,
    decimal PaidAmount,
    decimal DebtAmount,
    int OrderCount,
    int ProductCount,
    int LowStockCount);

public record DashboardSummaryDto(
    int ProductCount,
    int CustomerCount,
    int OrderCount,
    decimal Revenue,
    int InventoryUnits,
    decimal OutstandingDebt,
    int OccupiedTableCount,
    int AvailableTableCount);

public record FinanceSummaryDto(
    decimal TotalPaid,
    decimal OutstandingDebt,
    decimal CollectedToday,
    int DebtCount);

public record StaffDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string Status,
    List<string> Roles);

public record BranchSettingsDto(
    Guid Id,
    string Name,
    string Code,
    string? Phone,
    string? Address,
    string Status);

public record TenantSettingsDto(
    Guid TenantId,
    string Name,
    string Code,
    string Status,
    List<BranchSettingsDto> Branches);
