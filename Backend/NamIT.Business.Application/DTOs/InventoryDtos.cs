namespace NamIT.Business.Application.DTOs;

public record PurchaseItemRequest(
    Guid ProductId,
    int Quantity,
    decimal UnitCost);

public record CreatePurchaseRequest(
    Guid BranchId,
    Guid SupplierId,
    string PurchaseOrderNumber,
    DateTime PurchaseDate,
    IEnumerable<PurchaseItemRequest> Items);

public record PurchaseOrderResultDto(
    Guid Id,
    Guid BranchId,
    Guid SupplierId,
    string PurchaseOrderNumber,
    int TotalQuantity,
    decimal TotalAmount);

public record LowStockItemDto(
    Guid ProductId,
    string ProductName,
    int CurrentStock,
    int MinStock);