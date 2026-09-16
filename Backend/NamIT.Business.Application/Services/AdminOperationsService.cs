using Microsoft.EntityFrameworkCore;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Application.Services;

public interface IAdminOperationsService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<ReportSummaryDto> GetReportSummaryAsync();
    Task<FinanceSummaryDto> GetFinanceSummaryAsync();
    Task<List<StaffDto>> GetStaffAsync();
    Task<TenantSettingsDto> GetSettingsAsync();
}

public class AdminOperationsService : IAdminOperationsService
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public AdminOperationsService(IApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<ReportSummaryDto> GetReportSummaryAsync()
    {
        var orders = _db.Orders.AsNoTracking();
        var revenue = await orders.Where(order => order.Status == OrderStatus.Completed).SumAsync(order => (decimal?)order.Total) ?? 0;
        var paid = await orders.SumAsync(order => (decimal?)order.PaidAmount) ?? 0;
        var debt = await orders.SumAsync(order => (decimal?)order.DebtAmount) ?? 0;
        var productCount = await _db.Products.CountAsync();
        var lowStockCount = await GetLowStockCountAsync();
        return new ReportSummaryDto(revenue, paid, debt, await orders.CountAsync(), productCount, lowStockCount);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var orders = _db.Orders.AsNoTracking();
        var products = await _db.Products.AsNoTracking().Select(product => product.Id).ToListAsync();
        var inventoryUnits = await _db.InventoryTransactions.AsNoTracking()
            .SumAsync(transaction => (int?)(transaction.Type == Domain.Entities.InventoryTransactionType.SALE || transaction.Type == Domain.Entities.InventoryTransactionType.DAMAGE ? -transaction.Quantity : transaction.Quantity)) ?? 0;
        var revenue = await orders.Where(order => order.Status == OrderStatus.Completed).SumAsync(order => (decimal?)order.Total) ?? 0;
        var debt = await _db.Debts.AsNoTracking().SumAsync(item => (decimal?)item.Balance) ?? 0;
        var occupied = await _db.Tables.CountAsync(table => table.Status == DiningTableStatus.Occupied);
        var available = await _db.Tables.CountAsync(table => table.Status == DiningTableStatus.Available);
        return new DashboardSummaryDto(products.Count, await _db.Customers.CountAsync(), await orders.CountAsync(), revenue, inventoryUnits, debt, occupied, available);
    }

    public async Task<FinanceSummaryDto> GetFinanceSummaryAsync()
    {
        var totalPaid = await _db.Payments.SumAsync(payment => (decimal?)payment.Amount) ?? 0;
        var outstandingDebt = await _db.Debts.SumAsync(debt => (decimal?)debt.Balance) ?? 0;
        var startOfDay = DateTime.UtcNow.Date;
        var collectedToday = await _db.Payments.Where(payment => payment.PaidAt >= startOfDay).SumAsync(payment => (decimal?)payment.Amount) ?? 0;
        var debtCount = await _db.Debts.CountAsync(debt => debt.Balance > 0);
        return new FinanceSummaryDto(totalPaid, outstandingDebt, collectedToday, debtCount);
    }

    public async Task<List<StaffDto>> GetStaffAsync()
    {
        var users = await _db.Users.AsNoTracking().Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role).Where(user => !user.IsDeleted).OrderBy(user => user.FullName).ToListAsync();
        return users.Select(user => new StaffDto(user.Id, user.FullName, user.Email, user.Phone, user.Status.ToString(), user.UserRoles.Select(userRole => userRole.Role.Code).Distinct().ToList())).ToList();
    }

    public async Task<TenantSettingsDto> GetSettingsAsync()
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(item => item.Id == _tenantContext.TenantId)
            ?? throw new KeyNotFoundException("Không tìm thấy doanh nghiệp hiện tại.");
        var branches = await _db.Branches.AsNoTracking().Where(branch => branch.TenantId == tenant.Id && !branch.IsDeleted).OrderBy(branch => branch.Name).Select(branch => new BranchSettingsDto(branch.Id, branch.Name, branch.Code, branch.Phone, branch.Address, branch.Status.ToString())).ToListAsync();
        return new TenantSettingsDto(tenant.Id, tenant.Name, tenant.Code, tenant.Status.ToString(), branches);
    }

    private async Task<int> GetLowStockCountAsync()
    {
        var products = await _db.Products.AsNoTracking().Where(product => !product.IsDeleted).Select(product => new { product.Id, product.MinStock }).ToListAsync();
        var quantities = await _db.InventoryTransactions.AsNoTracking().GroupBy(transaction => transaction.ProductId).Select(group => new { ProductId = group.Key, Quantity = group.Sum(transaction => transaction.Type == Domain.Entities.InventoryTransactionType.SALE || transaction.Type == Domain.Entities.InventoryTransactionType.DAMAGE ? -transaction.Quantity : transaction.Quantity) }).ToDictionaryAsync(item => item.ProductId, item => item.Quantity);
        return products.Count(product => quantities.GetValueOrDefault(product.Id) < product.MinStock);
    }
}
