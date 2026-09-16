using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Api.Authorization;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/hospitality")]
public class HospitalityController : ControllerBase
{
    private readonly IHospitalityService _service;

    public HospitalityController(IHospitalityService service)
    {
        _service = service;
    }

    [HttpGet("tables")]
    [RequireModule(ModuleCode.TABLE, "TABLE_VIEW")]
    public async Task<IActionResult> GetTables() => Ok(await _service.GetTablesAsync());

    [HttpGet("tables/{tableId:guid}")]
    [RequireModule(ModuleCode.TABLE, "TABLE_VIEW")]
    public async Task<IActionResult> GetTableDetail(Guid tableId) => Ok(await _service.GetTableDetailAsync(tableId));

    [HttpPost("areas")]
    [RequireModule(ModuleCode.TABLE, "TABLE_CREATE")]
    public async Task<IActionResult> CreateArea([FromBody] CreateTableAreaRequest request) => Ok(await _service.CreateTableAreaAsync(request));

    [HttpPost("tables")]
    [RequireModule(ModuleCode.TABLE, "TABLE_CREATE")]
    public async Task<IActionResult> CreateTable([FromBody] CreateTableRequest request) => Ok(await _service.CreateTableAsync(request));

    [HttpPost("tables/{tableId:guid}/open")]
    [RequireModule(ModuleCode.TABLE)]
    public async Task<IActionResult> OpenTable(Guid tableId, [FromQuery] string? orderNumber)
        => Ok(await _service.OpenTableAsync(tableId, orderNumber));

    [HttpPost("tables/{tableId:guid}/items")]
    [RequireModule(ModuleCode.TABLE, "TABLE_UPDATE")]
    public async Task<IActionResult> AddItemToTable(Guid tableId, [FromBody] AddTableItemRequest request)
        => Ok(await _service.AddItemToTableAsync(tableId, request));

    [HttpPost("tables/{tableId:guid}/pay")]
    [RequireModule(ModuleCode.PAYMENT, "PAYMENT_CREATE")]
    public async Task<IActionResult> PayTable(Guid tableId, [FromBody] PayOrderRequest request)
        => Ok(await _service.PayTableAsync(tableId, request));

    [HttpPost("tables/{sourceTableId:guid}/items/transfer/{targetTableId:guid}")]
    [RequireModule(ModuleCode.TABLE, "TABLE_TRANSFER")]
    public async Task<IActionResult> TransferOrderItem(Guid sourceTableId, Guid targetTableId, [FromBody] TransferOrderItemRequest request)
    {
        await _service.TransferOrderItemAsync(sourceTableId, targetTableId, request);
        return NoContent();
    }

    [HttpPost("tables/{tableId:guid}/close")]
    [RequireModule(ModuleCode.TABLE, "TABLE_UPDATE")]
    public async Task<IActionResult> CloseTable(Guid tableId)
    {
        await _service.CloseTableAsync(tableId);
        return NoContent();
    }

    [HttpPost("tables/{sourceTableId:guid}/transfer/{targetTableId:guid}")]
    [RequireModule(ModuleCode.TABLE)]
    public async Task<IActionResult> TransferTable(Guid sourceTableId, Guid targetTableId)
    {
        await _service.TransferTableAsync(sourceTableId, targetTableId);
        return NoContent();
    }

    [HttpPost("tables/{sourceTableId:guid}/merge/{targetTableId:guid}")]
    [RequireModule(ModuleCode.TABLE)]
    public async Task<IActionResult> MergeTable(Guid sourceTableId, Guid targetTableId)
    {
        await _service.MergeTablesAsync(sourceTableId, targetTableId);
        return NoContent();
    }

    [HttpPost("tables/{tableId:guid}/split/{newTableId:guid}")]
    [RequireModule(ModuleCode.TABLE)]
    public async Task<IActionResult> SplitTable(Guid tableId, Guid newTableId, [FromBody] SplitTableRequest request)
    {
        await _service.SplitTableAsync(tableId, newTableId, request);
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/kitchen")]
    [RequireModule(ModuleCode.KITCHEN)]
    public async Task<IActionResult> CreateKitchenOrder(Guid orderId)
        => Ok(await _service.CreateKitchenOrderAsync(orderId));

    [HttpGet("kitchen")]
    [RequireModule(ModuleCode.KITCHEN, "KITCHEN_VIEW")]
    public async Task<IActionResult> GetKitchenOrders() => Ok(await _service.GetKitchenOrdersAsync());

    [HttpPost("kitchen/{kitchenOrderId:guid}/status/{status}")]
    [RequireModule(ModuleCode.KITCHEN, "KITCHEN_UPDATE")]
    public async Task<IActionResult> UpdateKitchenStatus(Guid kitchenOrderId, KitchenOrderStatus status)
    {
        await _service.UpdateKitchenStatusAsync(kitchenOrderId, status);
        return NoContent();
    }

    [HttpPost("toppings")]
    [RequireModule(ModuleCode.TOPPING)]
    public async Task<IActionResult> CreateTopping([FromBody] CreateToppingRequest request)
        => Ok(await _service.CreateToppingAsync(request));

    [HttpGet("toppings")]
    [RequireModule(ModuleCode.TOPPING, "PRODUCT_VIEW")]
    public async Task<IActionResult> GetToppings() => Ok(await _service.GetToppingsAsync());

    [HttpPost("combos")]
    [RequireModule(ModuleCode.COMBO)]
    public async Task<IActionResult> CreateCombo([FromBody] CreateComboRequest request)
        => Ok(await _service.CreateComboAsync(request));
}