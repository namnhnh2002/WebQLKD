using NamIT.Business.Application.DTOs;
using NamIT.Business.Domain.Entities;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto> GetByIdAsync(Guid id);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task DeleteAsync(Guid id);
}

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync();
    Task<ProductDto> GetByIdAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateProductRequest request);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request);
    Task DeleteAsync(Guid id);
}

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync();
    Task<CustomerDto> GetByIdAsync(Guid id);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request);
    Task DeleteAsync(Guid id);
}

public interface ISupplierService
{
    Task<List<SupplierDto>> GetAllAsync();
    Task<SupplierDto> GetByIdAsync(Guid id);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request);
    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request);
    Task DeleteAsync(Guid id);
}

public interface IInventoryService
{
    Task<PurchaseOrderResultDto> CreatePurchaseAsync(CreatePurchaseRequest request);
    Task<int> GetCurrentStockAsync(Guid productId);
    Task<List<LowStockItemDto>> GetLowStockAsync();
}

public interface IPosService
{
    Task<OrderResultDto> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderResultDto> PayExistingOrderAsync(Guid orderId, PayOrderRequest request, bool useTransaction = true);
    Task<List<OrderListItemDto>> GetOrdersAsync();
    Task<OrderDetailDto> GetOrderAsync(Guid id);
    Task CancelOrderAsync(Guid id);
}

public interface IHospitalityService
{
    Task<List<TableDto>> GetTablesAsync();
    Task<TableDetailDto> GetTableDetailAsync(Guid tableId);
    Task<TableArea> CreateTableAreaAsync(CreateTableAreaRequest request);
    Task<DiningTable> CreateTableAsync(CreateTableRequest request);
    Task<DiningTable> OpenTableAsync(Guid tableId, string? orderNumber);
    Task<TableDetailDto> AddItemToTableAsync(Guid tableId, AddTableItemRequest request);
    Task<OrderResultDto> PayTableAsync(Guid tableId, PayOrderRequest request);
    Task TransferOrderItemAsync(Guid sourceTableId, Guid targetTableId, TransferOrderItemRequest request);
    Task CloseTableAsync(Guid tableId);
    Task TransferTableAsync(Guid sourceTableId, Guid targetTableId);
    Task MergeTablesAsync(Guid sourceTableId, Guid targetTableId);
    Task SplitTableAsync(Guid tableId, Guid newTableId, SplitTableRequest request);
    Task<KitchenOrder> CreateKitchenOrderAsync(Guid orderId);
    Task<List<ToppingDto>> GetToppingsAsync();
    Task<List<KitchenOrderDto>> GetKitchenOrdersAsync();
    Task UpdateKitchenStatusAsync(Guid kitchenOrderId, KitchenOrderStatus status);
    Task<Topping> CreateToppingAsync(CreateToppingRequest request);
    Task<Combo> CreateComboAsync(CreateComboRequest request);
}
