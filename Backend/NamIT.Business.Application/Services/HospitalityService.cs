using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Domain.Entities;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Application.Services;

public class HospitalityService : IHospitalityService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<HospitalityService> _logger;
    private readonly ITenantContext? _tenantContext;
    private readonly IPosService? _posService;

    public HospitalityService(IApplicationDbContext db, ILogger<HospitalityService> logger, IPosService? posService = null, ITenantContext? tenantContext = null)
    {
        _db = db;
        _logger = logger;
        _tenantContext = tenantContext;
        _posService = posService;
    }

    public async Task<List<TableDto>> GetTablesAsync()
    {
        return await _db.Tables.AsNoTracking().Include(table => table.TableArea)
            .Select(table => new TableDto(table.Id, table.TableAreaId, table.TableArea.Name, table.Code, table.Name, table.Capacity, table.Status.ToString(), _db.TableOrders.Any(order => order.TableId == table.Id && order.IsActive)))
            .OrderBy(table => table.AreaName).ThenBy(table => table.Name).ToListAsync();
    }

    public async Task<TableDetailDto> GetTableDetailAsync(Guid tableId)
    {
        var table = await _db.Tables.AsNoTracking().Include(item => item.TableArea)
            .FirstOrDefaultAsync(item => item.Id == tableId)
            ?? throw new KeyNotFoundException("Không tìm thấy bàn.");
        var active = await _db.TableOrders.AsNoTracking()
            .Include(item => item.Order!).ThenInclude(order => order.Items).ThenInclude(item => item.Product)
            .Include(item => item.Order!).ThenInclude(order => order.Items).ThenInclude(item => item.Toppings).ThenInclude(topping => topping.Topping)
            .FirstOrDefaultAsync(item => item.TableId == tableId && item.IsActive);
        var order = active?.Order;
        return new TableDetailDto(table.Id, table.TableArea.Name, table.Name, table.Status.ToString(), active?.OpenedAt,
            order == null ? null : new OrderTableDto(order.Id, order.OrderNumber, order.Status.ToString(), order.Subtotal, order.Discount, order.Total,
                order.Items.Select(item => new OrderTableItemDto(item.Id, item.ProductId, item.Product.Name, item.Quantity, item.UnitPrice, item.LineTotal, item.Note,
                    item.Toppings.Select(topping => new OrderItemToppingDto(topping.Id, topping.ToppingId, topping.Topping.Name, topping.Quantity, topping.UnitPrice)).ToList())).ToList()));
    }

    public async Task<TableArea> CreateTableAreaAsync(CreateTableAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Mã và tên khu vực là bắt buộc.");
        var area = new TableArea { TenantId = Guid.Empty, BranchId = request.BranchId, Code = request.Code.Trim(), Name = request.Name.Trim() };
        _db.TableAreas.Add(area);
        await _db.SaveChangesAsync();
        return area;
    }

    public async Task<DiningTable> CreateTableAsync(CreateTableRequest request)
    {
        if (request.Capacity <= 0) throw new ArgumentException("Số chỗ phải lớn hơn 0.");
        if (!await _db.TableAreas.AnyAsync(area => area.Id == request.TableAreaId)) throw new KeyNotFoundException("Không tìm thấy khu vực.");
        var table = new DiningTable { TenantId = Guid.Empty, TableAreaId = request.TableAreaId, Code = request.Code.Trim(), Name = request.Name.Trim(), Capacity = request.Capacity };
        _db.Tables.Add(table);
        await _db.SaveChangesAsync();
        return table;
    }

    public async Task<DiningTable> OpenTableAsync(Guid tableId, string? orderNumber)
    {
        var table = await _db.Tables.FirstOrDefaultAsync(t => t.Id == tableId)
            ?? throw new KeyNotFoundException("Không tìm thấy bàn.");
        if (table.Status != DiningTableStatus.Available)
            throw new InvalidOperationException("Bàn hiện không sẵn sàng.");

        if (await _db.TableOrders.AnyAsync(order => order.TableId == tableId && order.IsActive))
            throw new InvalidOperationException("Bàn đã có order đang mở.");
        var branchId = await _db.TableAreas.Where(area => area.Id == table.TableAreaId).Select(area => area.BranchId).FirstOrDefaultAsync();
        branchId ??= await _db.Branches.Select(branch => (Guid?)branch.Id).FirstOrDefaultAsync();
        if (!branchId.HasValue) throw new InvalidOperationException("Tenant chưa có chi nhánh.");
        var order = new Order
        {
            TenantId = Guid.Empty,
            BranchId = branchId.Value,
            OrderNumber = string.IsNullOrWhiteSpace(orderNumber) ? $"TB-{DateTime.UtcNow:yyyyMMddHHmmss}-{table.Code}" : orderNumber,
            Status = OrderStatus.Draft
        };
        _db.Orders.Add(order);
        table.Status = DiningTableStatus.Occupied;
        _db.TableOrders.Add(new TableOrder
        {
            TenantId = Guid.Empty,
            TableId = tableId,
            OrderId = order.Id,
            ExternalOrderNumber = order.OrderNumber
        });
        AddAudit("OPEN_TABLE", nameof(DiningTable), tableId, table.Name);
        await _db.SaveChangesAsync();
        return table;
    }

    public async Task<TableDetailDto> AddItemToTableAsync(Guid tableId, AddTableItemRequest request)
    {
        if (request.Quantity <= 0) throw new ArgumentException("Số lượng phải lớn hơn 0.");
        var active = await _db.TableOrders.Include(item => item.Order!).ThenInclude(order => order.Items)
            .FirstOrDefaultAsync(item => item.TableId == tableId && item.IsActive)
            ?? throw new InvalidOperationException("Bàn chưa có order đang mở.");
        var product = await _db.Products.FirstOrDefaultAsync(item => item.Id == request.ProductId)
            ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm.");
        var stock = await _db.InventoryTransactions.Where(item => item.ProductId == product.Id)
            .SumAsync(item => item.Type == InventoryTransactionType.SALE || item.Type == InventoryTransactionType.DAMAGE ? -item.Quantity : item.Quantity);
        var existingQuantity = active.Order!.Items.Where(item => item.ProductId == product.Id).Sum(item => item.Quantity);
        if (stock < existingQuantity + request.Quantity) throw new InvalidOperationException("Sản phẩm không đủ tồn kho.");
        var toppingIds = request.ToppingIds?.Distinct().ToList() ?? [];
        var toppings = await _db.Toppings.Where(item => toppingIds.Contains(item.Id)).ToListAsync();
        if (toppings.Count != toppingIds.Count) throw new KeyNotFoundException("Một hoặc nhiều topping không tồn tại.");
        var toppingTotal = toppings.Sum(item => item.Price);
        var item = active.Order.Items.FirstOrDefault(orderItem => orderItem.ProductId == product.Id && orderItem.Note == request.Note && orderItem.Toppings.Count == 0);
        if (item == null)
        {
            item = new OrderItem { TenantId = Guid.Empty, OrderId = active.OrderId!.Value, ProductId = product.Id, Quantity = request.Quantity, UnitPrice = product.SellingPrice, Note = request.Note };
            item.LineTotal = (product.SellingPrice + toppingTotal) * request.Quantity;
            item.Toppings = toppings.Select(topping => new OrderItemTopping { TenantId = Guid.Empty, ToppingId = topping.Id, Quantity = 1, UnitPrice = topping.Price }).ToList();
            _db.OrderItems.Add(item);
        }
        else
        {
            item.Quantity += request.Quantity;
            item.LineTotal += (product.SellingPrice + toppingTotal) * request.Quantity;
        }
        active.Order.Subtotal = active.Order.Items.Sum(orderItem => orderItem.LineTotal);
        active.Order.Total = active.Order.Subtotal - active.Order.Discount;
        AddAudit("ADD_ITEM_TO_TABLE", nameof(OrderItem), item.Id, $"{product.Name} x{request.Quantity}");
        await _db.SaveChangesAsync();
        return await GetTableDetailAsync(tableId);
    }

    public async Task TransferOrderItemAsync(Guid sourceTableId, Guid targetTableId, TransferOrderItemRequest request)
    {
        if (sourceTableId == targetTableId || request.Quantity <= 0) throw new ArgumentException("Bàn và số lượng chuyển không hợp lệ.");
        var sourceOrder = await GetActiveOrderAsync(sourceTableId);
        var targetOrder = await GetActiveOrderAsync(targetTableId);
        var sourceItem = await _db.OrderItems.Include(item => item.Toppings).FirstOrDefaultAsync(item => item.Id == request.OrderItemId && item.OrderId == sourceOrder.Id)
            ?? throw new KeyNotFoundException("Không tìm thấy món trong order nguồn.");
        if (request.Quantity > sourceItem.Quantity) throw new InvalidOperationException("Số lượng chuyển vượt quá số lượng hiện có.");
        var targetItem = await _db.OrderItems.Include(item => item.Toppings).FirstOrDefaultAsync(item => item.OrderId == targetOrder.Id && item.ProductId == sourceItem.ProductId && item.Note == sourceItem.Note);
        if (request.Quantity == sourceItem.Quantity)
        {
            sourceItem.OrderId = targetOrder.Id;
            if (targetItem != null)
            {
                targetItem.Quantity += sourceItem.Quantity;
                targetItem.LineTotal += sourceItem.LineTotal;
                _db.OrderItems.Remove(sourceItem);
            }
        }
        else
        {
            var originalQuantity = sourceItem.Quantity;
            var unitLineTotal = sourceItem.LineTotal / originalQuantity;
            sourceItem.Quantity -= request.Quantity;
            sourceItem.LineTotal = unitLineTotal * sourceItem.Quantity;
            var moved = new OrderItem { TenantId = Guid.Empty, OrderId = targetOrder.Id, ProductId = sourceItem.ProductId, Quantity = request.Quantity, UnitPrice = sourceItem.UnitPrice, Note = sourceItem.Note, LineTotal = unitLineTotal * request.Quantity };
            foreach (var topping in sourceItem.Toppings)
                moved.Toppings.Add(new OrderItemTopping { TenantId = Guid.Empty, ToppingId = topping.ToppingId, Quantity = topping.Quantity, UnitPrice = topping.UnitPrice });
            _db.OrderItems.Add(moved);
        }
        RecalculateOrder(sourceOrder);
        RecalculateOrder(targetOrder);
        AddAudit("TRANSFER_ORDER_ITEM", nameof(OrderItem), request.OrderItemId, $"{sourceTableId}->{targetTableId} x{request.Quantity}");
        await _db.SaveChangesAsync();
    }

    public async Task<OrderResultDto> PayTableAsync(Guid tableId, PayOrderRequest request)
    {
        return await _db.ExecuteInTransactionAsync(async () =>
        {
            var active = await _db.TableOrders.FirstOrDefaultAsync(item => item.TableId == tableId && item.IsActive && item.OrderId.HasValue)
                ?? throw new InvalidOperationException("Bàn chưa có order đang mở.");
            if (_posService == null) throw new InvalidOperationException("Payment service chưa được cấu hình.");
            var result = await _posService.PayExistingOrderAsync(active.OrderId!.Value, request, false);
            active.IsActive = false;
            active.ClosedAt = DateTime.UtcNow;
            var table = await _db.Tables.FirstAsync(item => item.Id == tableId);
            table.Status = DiningTableStatus.Available;
            AddAudit("CLOSE_TABLE", nameof(DiningTable), tableId, "PAY_TABLE");
            await _db.SaveChangesAsync();
            return result;
        });
    }

    public async Task CloseTableAsync(Guid tableId)
    {
        var table = await _db.Tables.FirstOrDefaultAsync(item => item.Id == tableId) ?? throw new KeyNotFoundException("Không tìm thấy bàn.");
        var activeOrder = await _db.TableOrders.FirstOrDefaultAsync(order => order.TableId == tableId && order.IsActive);
        if (activeOrder != null)
        {
            activeOrder.IsActive = false;
            activeOrder.ClosedAt = DateTime.UtcNow;
        }
        table.Status = DiningTableStatus.Available;
        AddAudit("CLOSE_TABLE", nameof(DiningTable), tableId, table.Name);
        await _db.SaveChangesAsync();
    }

    public async Task TransferTableAsync(Guid sourceTableId, Guid targetTableId)
    {
        if (sourceTableId == targetTableId)
            throw new ArgumentException("Bàn nguồn và bàn đích phải khác nhau.");

        var source = await _db.Tables.FirstOrDefaultAsync(t => t.Id == sourceTableId)
            ?? throw new KeyNotFoundException("Không tìm thấy bàn nguồn.");
        var target = await _db.Tables.FirstOrDefaultAsync(t => t.Id == targetTableId)
            ?? throw new KeyNotFoundException("Không tìm thấy bàn đích.");
        if (source.Status != DiningTableStatus.Occupied)
            throw new InvalidOperationException("Bàn nguồn không có order đang mở.");
        if (target.Status != DiningTableStatus.Available)
            throw new InvalidOperationException("Bàn đích hiện không sẵn sàng.");

        var activeOrder = await _db.TableOrders.FirstOrDefaultAsync(t => t.TableId == sourceTableId && t.IsActive)
            ?? throw new InvalidOperationException("Không tìm thấy order bàn đang mở.");
        activeOrder.TableId = targetTableId;
        source.Status = DiningTableStatus.Available;
        target.Status = DiningTableStatus.Occupied;
        AddAudit("TRANSFER_TABLE", nameof(DiningTable), sourceTableId, $"{sourceTableId}->{targetTableId}");
        await _db.SaveChangesAsync();
    }

    public async Task MergeTablesAsync(Guid sourceTableId, Guid targetTableId)
    {
        if (sourceTableId == targetTableId)
            throw new ArgumentException("Bàn nguồn và bàn đích phải khác nhau.");

        var source = await _db.Tables.FirstOrDefaultAsync(t => t.Id == sourceTableId)
            ?? throw new KeyNotFoundException("Không tìm thấy bàn nguồn.");
        var target = await _db.Tables.FirstOrDefaultAsync(t => t.Id == targetTableId)
            ?? throw new KeyNotFoundException("Không tìm thấy bàn đích.");
        if (source.Status != DiningTableStatus.Occupied || target.Status != DiningTableStatus.Occupied)
            throw new InvalidOperationException("Cả hai bàn phải đang có order để gộp.");

        var sourceTableOrder = await _db.TableOrders
            .Include(item => item.Order).ThenInclude(order => order!.Items)
            .Where(item => item.TableId == sourceTableId && item.IsActive)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Không tìm thấy order nguồn đang mở.");
        var targetTableOrder = await _db.TableOrders
            .Include(item => item.Order).ThenInclude(order => order!.Items)
            .Where(item => item.TableId == targetTableId && item.IsActive)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Không tìm thấy order đích đang mở.");
        var sourceOrder = sourceTableOrder.Order ?? throw new InvalidOperationException("Order nguồn không tồn tại.");
        var targetOrder = targetTableOrder.Order ?? throw new InvalidOperationException("Order đích không tồn tại.");
        foreach (var item in sourceOrder.Items)
            item.OrderId = targetOrder.Id;
        targetOrder.Subtotal = targetOrder.Items.Sum(item => item.LineTotal);
        targetOrder.Total = targetOrder.Subtotal - targetOrder.Discount;
        sourceOrder.Status = OrderStatus.Cancelled;
        sourceTableOrder.IsActive = false;
        sourceTableOrder.ClosedAt = DateTime.UtcNow;

        source.Status = DiningTableStatus.Available;
        AddAudit("MERGE_TABLE", nameof(DiningTable), sourceTableId, $"{sourceTableId}+{targetTableId}");
        AddAudit("MERGE_ORDER", nameof(Order), sourceOrder.Id, $"{sourceOrder.Id}->{targetOrder.Id}");
        await _db.SaveChangesAsync();
    }

    public async Task SplitTableAsync(Guid tableId, Guid newTableId, SplitTableRequest request)
    {
        if (tableId == newTableId)
            throw new ArgumentException("Bàn hiện tại và bàn mới phải khác nhau.");

        var table = await _db.Tables.FirstOrDefaultAsync(t => t.Id == tableId)
            ?? throw new KeyNotFoundException("Không tìm thấy bàn hiện tại.");
        var newTable = await _db.Tables.FirstOrDefaultAsync(t => t.Id == newTableId)
            ?? throw new KeyNotFoundException("Không tìm thấy bàn mới.");
        if (table.Status != DiningTableStatus.Occupied)
            throw new InvalidOperationException("Bàn hiện tại không có order đang mở.");
        if (newTable.Status != DiningTableStatus.Available)
            throw new InvalidOperationException("Bàn mới hiện không sẵn sàng.");

        var activeOrder = await _db.TableOrders.Include(item => item.Order).FirstOrDefaultAsync(t => t.TableId == tableId && t.IsActive)
            ?? throw new InvalidOperationException("Không tìm thấy order bàn đang mở.");
        var selected = request.Items?.ToList() ?? [];
        if (!selected.Any()) throw new ArgumentException("Phải chọn ít nhất một món để tách.");
        var order = activeOrder.Order ?? throw new InvalidOperationException("Order bàn không tồn tại.");
        var selectedIds = selected.Select(item => item.OrderItemId).ToHashSet();
        var items = await _db.OrderItems.Include(item => item.Toppings).Where(item => item.OrderId == order.Id && selectedIds.Contains(item.Id)).ToListAsync();
        if (items.Count != selected.Count || selected.Any(item => item.Quantity <= 0 || items.First(source => source.Id == item.OrderItemId).Quantity < item.Quantity)) throw new InvalidOperationException("Số lượng món tách không hợp lệ.");
        var branchId = order.BranchId;
        var newOrder = new Order { TenantId = Guid.Empty, BranchId = branchId, OrderNumber = $"{order.OrderNumber}-SPLIT", Status = OrderStatus.Draft };
        _db.Orders.Add(newOrder);
        foreach (var selection in selected)
        {
            var source = items.First(item => item.Id == selection.OrderItemId);
            var originalQuantity = source.Quantity;
            var unitLineTotal = source.LineTotal / originalQuantity;
            source.Quantity -= selection.Quantity;
            source.LineTotal = unitLineTotal * source.Quantity;
            var moved = new OrderItem { TenantId = Guid.Empty, OrderId = newOrder.Id, ProductId = source.ProductId, Quantity = selection.Quantity, UnitPrice = source.UnitPrice, LineTotal = unitLineTotal * selection.Quantity, Note = source.Note };
            foreach (var topping in source.Toppings) moved.Toppings.Add(new OrderItemTopping { TenantId = Guid.Empty, ToppingId = topping.ToppingId, Quantity = topping.Quantity, UnitPrice = topping.UnitPrice });
            _db.OrderItems.Add(moved);
        }
        RecalculateOrder(order);
        RecalculateOrder(newOrder);
        _db.TableOrders.Add(new TableOrder
        {
            TenantId = Guid.Empty,
            TableId = newTableId,
            OrderId = newOrder.Id,
            ExternalOrderNumber = newOrder.OrderNumber,
            IsActive = true
        });
        newTable.Status = DiningTableStatus.Occupied;
        AddAudit("SPLIT_TABLE", nameof(DiningTable), tableId, $"{tableId}->{newTableId}");
        AddAudit("SPLIT_ORDER", nameof(Order), order.Id, newOrder.Id.ToString());
        await _db.SaveChangesAsync();
    }

    private async Task<Order> GetActiveOrderAsync(Guid tableId)
    {
        var table = await _db.Tables.FirstOrDefaultAsync(item => item.Id == tableId) ?? throw new KeyNotFoundException("Không tìm thấy bàn.");
        if (table.Status != DiningTableStatus.Occupied) throw new InvalidOperationException("Bàn chưa có order đang mở.");
        var relation = await _db.TableOrders.FirstOrDefaultAsync(item => item.TableId == tableId && item.IsActive && item.OrderId.HasValue)
            ?? throw new InvalidOperationException("Không tìm thấy order bàn đang mở.");
        return await _db.Orders.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == relation.OrderId!.Value)
            ?? throw new KeyNotFoundException("Order bàn không tồn tại.");
    }

    private static void RecalculateOrder(Order order)
    {
        order.Subtotal = order.Items.Sum(item => item.LineTotal);
        order.Total = order.Subtotal - order.Discount;
    }

    public async Task<KitchenOrder> CreateKitchenOrderAsync(Guid orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(item => item.Product)
            .Include(o => o.Items).ThenInclude(item => item.Toppings)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new KeyNotFoundException("Không tìm thấy order.");
        var sentItemIds = await _db.KitchenOrderItems.Where(item => item.KitchenOrder.OrderId == orderId).Select(item => item.OrderItemId).ToListAsync();
        var pendingItems = order.Items.Where(item => item.Product.Station != ProductStation.None && !sentItemIds.Contains(item.Id)).ToList();
        if (!pendingItems.Any()) throw new InvalidOperationException("Không có món mới cần gửi bếp/bar.");

        var kitchenOrder = new KitchenOrder
        {
            TenantId = Guid.Empty,
            OrderId = orderId,
            Items = pendingItems.Select(item => new KitchenOrderItem
            {
                TenantId = Guid.Empty,
                ProductId = item.ProductId,
                OrderItemId = item.Id,
                Quantity = item.Quantity,
                Note = item.Note
            }).ToList()
        };
        _db.KitchenOrders.Add(kitchenOrder);
        AddAudit("SEND_TO_KITCHEN", nameof(KitchenOrder), kitchenOrder.Id, orderId.ToString());
        await _db.SaveChangesAsync();
        return kitchenOrder;
    }

    public async Task<List<KitchenOrderDto>> GetKitchenOrdersAsync()
    {
        var orders = await _db.KitchenOrders.AsNoTracking().Include(order => order.Items).ThenInclude(item => item.Product).Include(order => order.Items).ThenInclude(item => item.OrderItem).ThenInclude(item => item.Toppings).ThenInclude(item => item.Topping).OrderBy(order => order.CreatedAt).ToListAsync();
        return orders.Select(order => new KitchenOrderDto(order.Id, order.OrderId, order.Status.ToString(), order.CreatedAt, order.Items.Select(item => new KitchenOrderItemDto(item.ProductId, item.Product.Name, item.Quantity, item.Note, item.Product.Station.ToString(), item.OrderItem.Toppings.Select(topping => topping.Topping.Name).ToList())).ToList())).ToList();
    }

    public async Task<List<ToppingDto>> GetToppingsAsync()
        => await _db.Toppings.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new ToppingDto(item.Id, item.Code, item.Name, item.Price)).ToListAsync();

    public async Task UpdateKitchenStatusAsync(Guid kitchenOrderId, KitchenOrderStatus status)
    {
        var kitchenOrder = await _db.KitchenOrders.FirstOrDefaultAsync(order => order.Id == kitchenOrderId) ?? throw new KeyNotFoundException("Không tìm thấy kitchen order.");
        if (!Enum.IsDefined(status)) throw new ArgumentException("Trạng thái bếp không hợp lệ.");
        kitchenOrder.Status = status;
        if (status == KitchenOrderStatus.Preparing) kitchenOrder.StartedAt ??= DateTime.UtcNow;
        if (status is KitchenOrderStatus.Ready or KitchenOrderStatus.Served) kitchenOrder.CompletedAt ??= DateTime.UtcNow;
        AddAudit("UPDATE_KITCHEN_STATUS", nameof(KitchenOrder), kitchenOrderId, status.ToString());
        await _db.SaveChangesAsync();
    }

    private void AddAudit(string action, string entityName, Guid entityId, string? detail)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = Guid.Empty,
            UserId = _tenantContext?.UserId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId.ToString(),
            Detail = detail
        });
    }

    public async Task<Topping> CreateToppingAsync(CreateToppingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Price < 0)
            throw new ArgumentException("Thông tin topping không hợp lệ.");

        var topping = new Topping { TenantId = Guid.Empty, Code = request.Code, Name = request.Name, Price = request.Price };
        _db.Toppings.Add(topping);
        await _db.SaveChangesAsync();
        return topping;
    }

    public async Task<Combo> CreateComboAsync(CreateComboRequest request)
    {
        var items = request.Items?.ToList() ?? new List<ComboItemRequest>();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Price < 0 || !items.Any())
            throw new ArgumentException("Thông tin combo không hợp lệ.");
        if (items.Any(item => item.Quantity <= 0))
            throw new ArgumentException("Số lượng sản phẩm trong combo phải lớn hơn 0.");

        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        if (await _db.Products.CountAsync(p => productIds.Contains(p.Id)) != productIds.Count)
            throw new KeyNotFoundException("Một hoặc nhiều sản phẩm trong combo không tồn tại.");

        var combo = new Combo
        {
            TenantId = Guid.Empty,
            Code = request.Code,
            Name = request.Name,
            Price = request.Price,
            Items = items.Select(item => new ComboItem
            {
                TenantId = Guid.Empty,
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };
        _db.Combos.Add(combo);
        await _db.SaveChangesAsync();
        return combo;
    }
}