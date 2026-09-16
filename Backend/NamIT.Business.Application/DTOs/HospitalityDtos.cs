namespace NamIT.Business.Application.DTOs;

public record CreateToppingRequest(string Code, string Name, decimal Price);
public record ToppingDto(Guid Id, string Code, string Name, decimal Price);

public record ComboItemRequest(Guid ProductId, int Quantity);

public record CreateComboRequest(string Code, string Name, decimal Price, IEnumerable<ComboItemRequest> Items);

public record TableDto(Guid Id, Guid TableAreaId, string AreaName, string Code, string Name, int Capacity, string Status, bool HasActiveOrder);
public record TableDetailDto(Guid Id, string AreaName, string Name, string Status, DateTime? OpenedAt, OrderTableDto? Order);
public record OrderTableDto(Guid Id, string OrderNumber, string Status, decimal Subtotal, decimal Discount, decimal Total, List<OrderTableItemDto> Items);
public record OrderTableItemDto(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal, string? Note, List<OrderItemToppingDto> Toppings);
public record OrderItemToppingDto(Guid Id, Guid ToppingId, string Name, int Quantity, decimal UnitPrice);
public record AddTableItemRequest(Guid ProductId, int Quantity, string? Note, IEnumerable<Guid>? ToppingIds);
public record TransferOrderItemRequest(Guid OrderItemId, int Quantity);
public record SplitTableRequest(IEnumerable<TransferOrderItemRequest> Items);
public record CreateTableAreaRequest(Guid? BranchId, string Code, string Name);
public record CreateTableRequest(Guid TableAreaId, string Code, string Name, int Capacity);
public record KitchenOrderDto(Guid Id, Guid OrderId, string Status, DateTime CreatedAt, List<KitchenOrderItemDto> Items);
public record KitchenOrderItemDto(Guid ProductId, string ProductName, int Quantity, string? Note, string Station, List<string> Toppings);