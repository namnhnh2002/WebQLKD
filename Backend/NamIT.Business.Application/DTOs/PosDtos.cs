using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Application.DTOs;

public record CartItemRequest(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreatePaymentRequest(decimal Amount, PaymentMethod Method, string? Reference);

public record CreateOrderRequest(
    Guid BranchId,
    Guid? CustomerId,
    IEnumerable<CartItemRequest> Items,
    decimal Discount,
    IEnumerable<CreatePaymentRequest> Payments,
    decimal DiscountPercent = 0);

public record PayOrderRequest(Guid? CustomerId, IEnumerable<CreatePaymentRequest> Payments);

public record OrderResultDto(
    Guid Id,
    string OrderNumber,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    decimal PaidAmount,
    decimal DebtAmount,
    string ReceiptNumber);

public record OrderListItemDto(
    Guid Id,
    string OrderNumber,
    DateTime CreatedAt,
    string CustomerName,
    decimal Total,
    decimal PaidAmount,
    decimal DebtAmount,
    string Status,
    string PaymentMethods);

public record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    DateTime CreatedAt,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    decimal PaidAmount,
    decimal DebtAmount,
    string Status,
    List<OrderDetailItemDto> Items);

public record OrderDetailItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);