using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Domain.Entities;

namespace NamIT.Business.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(IApplicationDbContext db, ILogger<InventoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PurchaseOrderResultDto> CreatePurchaseAsync(CreatePurchaseRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var items = request.Items?.ToList() ?? new List<PurchaseItemRequest>();
        if (!items.Any())
            throw new ArgumentException("Phải có ít nhất một sản phẩm trong đơn nhập.", nameof(request));

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                throw new ArgumentException("Số lượng nhập phải lớn hơn 0.");

            if (!await _db.Products.AnyAsync(p => p.Id == item.ProductId))
                throw new KeyNotFoundException($"Không tìm thấy sản phẩm {item.ProductId}.");
        }

        var purchaseId = Guid.NewGuid();
        var transactions = items.Select(item => new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            BranchId = request.BranchId,
            ProductId = item.ProductId,
            Type = InventoryTransactionType.PURCHASE,
            Quantity = item.Quantity,
            ReferenceId = purchaseId,
            Note = request.PurchaseOrderNumber,
            CreatedBy = null
        }).ToList();

        _db.InventoryTransactions.AddRange(transactions);
        await _db.SaveChangesAsync();

        return new PurchaseOrderResultDto(
            purchaseId,
            request.BranchId,
            request.SupplierId,
            request.PurchaseOrderNumber,
            items.Sum(x => x.Quantity),
            items.Sum(x => x.Quantity * x.UnitCost));
    }

    public async Task<int> GetCurrentStockAsync(Guid productId)
    {
        var total = await _db.InventoryTransactions
            .Where(t => t.ProductId == productId)
            .Select(t => (int?)GetTransactionDelta(t.Type, t.Quantity) ?? 0)
            .SumAsync();

        return total;
    }

    public async Task<List<LowStockItemDto>> GetLowStockAsync()
    {
        var products = await _db.Products
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var results = new List<LowStockItemDto>();

        foreach (var product in products)
        {
            var currentStock = await GetCurrentStockAsync(product.Id);
            if (currentStock < product.MinStock)
            {
                results.Add(new LowStockItemDto(product.Id, product.Name, currentStock, product.MinStock));
            }
        }

        return results;
    }

    private static int GetTransactionDelta(InventoryTransactionType type, int quantity)
    {
        return type switch
        {
            InventoryTransactionType.PURCHASE => quantity,
            InventoryTransactionType.RETURN => quantity,
            InventoryTransactionType.ADJUSTMENT => quantity,
            InventoryTransactionType.SALE => -quantity,
            InventoryTransactionType.DAMAGE => -quantity,
            InventoryTransactionType.TRANSFER => quantity,
            _ => 0
        };
    }
}