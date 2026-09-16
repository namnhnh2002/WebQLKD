using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Api.Authorization;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Api.Controllers;

[Authorize]
[Route("api/inventory")]
[RequireModule(ModuleCode.INVENTORY)]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost("purchase")]
    public async Task<ActionResult<PurchaseOrderResultDto>> CreatePurchase([FromBody] CreatePurchaseRequest request)
    {
        var result = await _inventoryService.CreatePurchaseAsync(request);
        return Ok(result);
    }

    [HttpGet("stock/{productId:guid}")]
    public async Task<ActionResult<int>> GetCurrentStock(Guid productId)
    {
        return Ok(await _inventoryService.GetCurrentStockAsync(productId));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<LowStockItemDto>>> GetLowStock()
    {
        return Ok(await _inventoryService.GetLowStockAsync());
    }
}