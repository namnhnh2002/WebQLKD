using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Domain.Entities;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Application.Services;

public class PosService : IPosService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<PosService> _logger;
    private readonly ITenantContext? _tenantContext;

    public PosService(IApplicationDbContext db, ILogger<PosService> logger, ITenantContext? tenantContext = null)
    {
        _db = db;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public Task<OrderResultDto> CreateOrderAsync(CreateOrderRequest request)
        => _db.ExecuteInTransactionAsync(() => CreateOrderInternalAsync(request));

    public Task<OrderResultDto> PayExistingOrderAsync(Guid orderId, PayOrderRequest request, bool useTransaction = true)
    {
        var operation = PayExistingOrderInternalAsync(orderId, request);
        return useTransaction ? _db.ExecuteInTransactionAsync(() => operation) : operation;
    }

    private async Task<OrderResultDto> PayExistingOrderInternalAsync(Guid orderId, PayOrderRequest request)

        {
            var order = await _db.Orders.Include(item => item.Items).Include(item => item.Payments).FirstOrDefaultAsync(item => item.Id == orderId)
                ?? throw new KeyNotFoundException("Không tìm thấy order.");
            if (order.Status == OrderStatus.Completed)
            {
                _logger.LogWarning("Duplicate payment rejected for completed order {OrderId}", orderId);
                throw new InvalidOperationException("Order đã được thanh toán.");
            }
            var payments = request.Payments?.ToList() ?? [];
            if (payments.Any(item => item.Amount <= 0 || !Enum.IsDefined(item.Method))) throw new ArgumentException("Thông tin thanh toán không hợp lệ.");
            if (request.CustomerId.HasValue && !await _db.Customers.AnyAsync(item => item.Id == request.CustomerId.Value)) throw new KeyNotFoundException("Không tìm thấy khách hàng.");
            var paid = payments.Sum(item => item.Amount);
            if (paid > order.Total) throw new ArgumentException("Số tiền thanh toán không được vượt quá tổng đơn hàng.");
            var debt = order.Total - paid;
            if (debt > 0 && !request.CustomerId.HasValue) throw new InvalidOperationException("Muốn ghi công nợ phải chọn khách hàng.");
            foreach (var payment in payments) _db.Payments.Add(new Payment { TenantId = Guid.Empty, OrderId = order.Id, Amount = payment.Amount, Method = payment.Method, Reference = payment.Reference });
            if (debt > 0)
            {
                order.CustomerId = request.CustomerId;
                _db.Debts.Add(new Debt { TenantId = Guid.Empty, OrderId = order.Id, CustomerId = request.CustomerId, OriginalAmount = debt, Balance = debt, Status = DebtStatus.Open });
                _db.DebtTransactions.Add(new DebtTransaction { TenantId = Guid.Empty, CustomerId = request.CustomerId, ReferenceId = order.Id, Type = DebtTransactionType.SALE_DEBT, Amount = debt, Note = order.OrderNumber, CreatedBy = _tenantContext?.UserId });
            }
            foreach (var item in order.Items) _db.InventoryTransactions.Add(new InventoryTransaction { TenantId = Guid.Empty, BranchId = order.BranchId, ProductId = item.ProductId, Type = InventoryTransactionType.SALE, Quantity = item.Quantity, ReferenceId = order.Id, Note = order.OrderNumber });
            order.PaidAmount = paid;
            order.DebtAmount = debt;
            order.Status = OrderStatus.Completed;
            _db.Receipts.Add(new Receipt { TenantId = Guid.Empty, OrderId = order.Id, ReceiptNumber = $"RC-{DateTime.UtcNow:yyyyMMddHHmmss}-{order.Id.ToString()[..6].ToUpperInvariant()}" });
            _db.AuditLogs.Add(new AuditLog { TenantId = Guid.Empty, UserId = _tenantContext?.UserId, Action = "PAY_TABLE", EntityName = nameof(Order), EntityId = order.Id.ToString(), Detail = order.OrderNumber });
            await _db.SaveChangesAsync();
            return new OrderResultDto(order.Id, order.OrderNumber, order.Subtotal, order.Discount, order.Total, paid, debt, order.OrderNumber);
        }

    private async Task<OrderResultDto> CreateOrderInternalAsync(CreateOrderRequest request)
    {
        var items = request.Items?.ToList() ?? new List<CartItemRequest>();
        var payments = request.Payments?.ToList() ?? new List<CreatePaymentRequest>();
        if (!items.Any()) throw new ArgumentException("Đơn hàng phải có ít nhất một sản phẩm.", nameof(request));
        if (request.Discount < 0 || request.DiscountPercent < 0 || request.DiscountPercent > 100) throw new ArgumentException("Giảm giá không hợp lệ.");
        if (request.Discount > 0 && request.DiscountPercent > 0) throw new ArgumentException("Chỉ được sử dụng một loại giảm giá.");
        if (!await _db.Branches.AnyAsync(branch => branch.Id == request.BranchId)) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");
        if (request.CustomerId.HasValue && !await _db.Customers.AnyAsync(customer => customer.Id == request.CustomerId.Value)) throw new KeyNotFoundException("Không tìm thấy khách hàng.");

        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(product => productIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id);
        if (products.Count != productIds.Count) throw new KeyNotFoundException("Một hoặc nhiều sản phẩm không tồn tại.");

        var orderItems = new List<OrderItem>();
        foreach (var item in items)
        {
            if (item.Quantity <= 0) throw new ArgumentException("Số lượng sản phẩm phải lớn hơn 0.");
            var currentStock = await GetCurrentStockAsync(item.ProductId);
            if (currentStock < item.Quantity) throw new InvalidOperationException($"Sản phẩm {products[item.ProductId].Name} không đủ tồn kho.");
            var unitPrice = products[item.ProductId].SellingPrice;
            orderItems.Add(new OrderItem { TenantId = Guid.Empty, ProductId = item.ProductId, Quantity = item.Quantity, UnitPrice = unitPrice, LineTotal = unitPrice * item.Quantity });
        }

        var subtotal = orderItems.Sum(item => item.LineTotal);
        var discountAmount = request.DiscountPercent > 0 ? Math.Round(subtotal * request.DiscountPercent / 100m, 2) : request.Discount;
        var total = subtotal - discountAmount;
        if (total < 0) throw new ArgumentException("Giảm giá không thể lớn hơn giá trị đơn hàng.");
        if (payments.Any(payment => payment.Amount <= 0 || !Enum.IsDefined(payment.Method))) throw new ArgumentException("Thông tin thanh toán không hợp lệ.");

        var paidAmount = payments.Sum(payment => payment.Amount);
        if (paidAmount > total) throw new ArgumentException("Số tiền thanh toán không được vượt quá tổng đơn hàng.");
        var debtAmount = total - paidAmount;
        if (debtAmount > 0 && !request.CustomerId.HasValue) throw new InvalidOperationException("Muốn ghi công nợ phải chọn khách hàng.");

        var orderId = Guid.NewGuid();
        var orderNumber = $"SO-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId.ToString()[..6].ToUpperInvariant()}";
        var receiptNumber = $"RC-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId.ToString()[..6].ToUpperInvariant()}";
        _db.Orders.Add(new Order { Id = orderId, TenantId = Guid.Empty, BranchId = request.BranchId, CustomerId = request.CustomerId, OrderNumber = orderNumber, Status = OrderStatus.Completed, Subtotal = subtotal, Discount = discountAmount, Total = total, PaidAmount = paidAmount, DebtAmount = debtAmount, Items = orderItems });

        foreach (var payment in payments)
            _db.Payments.Add(new Payment { TenantId = Guid.Empty, OrderId = orderId, Amount = payment.Amount, Method = payment.Method, Reference = payment.Reference });

        if (debtAmount > 0)
        {
            _db.Debts.Add(new Debt { TenantId = Guid.Empty, OrderId = orderId, CustomerId = request.CustomerId, OriginalAmount = debtAmount, Balance = debtAmount, Status = DebtStatus.Open });
            _db.DebtTransactions.Add(new DebtTransaction { TenantId = Guid.Empty, CustomerId = request.CustomerId, ReferenceId = orderId, Type = DebtTransactionType.SALE_DEBT, Amount = debtAmount, Note = orderNumber, CreatedBy = _tenantContext?.UserId });
        }

        foreach (var item in orderItems)
            _db.InventoryTransactions.Add(new InventoryTransaction { TenantId = Guid.Empty, BranchId = request.BranchId, ProductId = item.ProductId, Type = InventoryTransactionType.SALE, Quantity = item.Quantity, ReferenceId = orderId, Note = orderNumber });

        _db.Receipts.Add(new Receipt { TenantId = Guid.Empty, OrderId = orderId, ReceiptNumber = receiptNumber });
        _db.AuditLogs.Add(new AuditLog { TenantId = Guid.Empty, UserId = _tenantContext?.UserId, Action = "CREATE_ORDER", EntityName = nameof(Order), EntityId = orderId.ToString(), Detail = orderNumber });
        await _db.SaveChangesAsync();
        return new OrderResultDto(orderId, orderNumber, subtotal, discountAmount, total, paidAmount, debtAmount, receiptNumber);
    }

    public async Task<List<OrderListItemDto>> GetOrdersAsync()
    {
        var orders = await _db.Orders.AsNoTracking().Include(order => order.Customer).Include(order => order.Payments).OrderByDescending(order => order.CreatedAt).ToListAsync();
        return orders.Select(order => new OrderListItemDto(order.Id, order.OrderNumber, order.CreatedAt, order.Customer?.Name ?? "Khách lẻ", order.Total, order.PaidAmount, order.DebtAmount, order.Status.ToString(), string.Join(", ", order.Payments.Select(payment => payment.Method.ToString()).Distinct()))).ToList();
    }

    public async Task<OrderDetailDto> GetOrderAsync(Guid id)
    {
        var order = await _db.Orders.Include(item => item.Items).ThenInclude(item => item.Product).FirstOrDefaultAsync(item => item.Id == id) ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        return new OrderDetailDto(order.Id, order.OrderNumber, order.CreatedAt, order.Subtotal, order.Discount, order.Total, order.PaidAmount, order.DebtAmount, order.Status.ToString(), order.Items.Select(item => new OrderDetailItemDto(item.ProductId, item.Product.Name, item.Quantity, item.UnitPrice, item.LineTotal)).ToList());
    }

    public Task CancelOrderAsync(Guid id) => _db.ExecuteInTransactionAsync(async () =>
    {
        var order = await _db.Orders.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == id) ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
        if (order.Status == OrderStatus.Cancelled) throw new InvalidOperationException("Đơn hàng đã được hủy.");
        order.Status = OrderStatus.Cancelled;
        foreach (var item in order.Items)
            _db.InventoryTransactions.Add(new InventoryTransaction { TenantId = Guid.Empty, BranchId = order.BranchId, ProductId = item.ProductId, Type = InventoryTransactionType.RETURN, Quantity = item.Quantity, ReferenceId = order.Id, Note = $"CANCEL:{order.OrderNumber}" });
        _db.AuditLogs.Add(new AuditLog { TenantId = Guid.Empty, UserId = _tenantContext?.UserId, Action = "CANCEL_ORDER", EntityName = nameof(Order), EntityId = order.Id.ToString(), Detail = order.OrderNumber });
        await _db.SaveChangesAsync();
        return true;
    });

    private async Task<int> GetCurrentStockAsync(Guid productId)
        => await _db.InventoryTransactions.Where(transaction => transaction.ProductId == productId).SumAsync(transaction => transaction.Type == InventoryTransactionType.SALE || transaction.Type == InventoryTransactionType.DAMAGE ? -transaction.Quantity : transaction.Quantity);
}
