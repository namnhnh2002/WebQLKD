using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Api.Authorization;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pos")]
[RequireModule(ModuleCode.POS, "ORDER_CREATE")]
public class PosController : ControllerBase
{
    private readonly IPosService _posService;

    public PosController(IPosService posService)
    {
        _posService = posService;
    }

    [HttpPost("orders")]
    [RequireModule(ModuleCode.PAYMENT, "PAYMENT_CREATE")]
    public async Task<ActionResult<OrderResultDto>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        return Ok(await _posService.CreateOrderAsync(request));
    }

    [HttpGet("orders")]
    [RequireModule(ModuleCode.POS, "ORDER_VIEW")]
    public async Task<ActionResult<List<OrderListItemDto>>> GetOrders() => Ok(await _posService.GetOrdersAsync());

    [HttpGet("orders/{id:guid}")]
    [RequireModule(ModuleCode.POS, "ORDER_VIEW")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(Guid id) => Ok(await _posService.GetOrderAsync(id));

    [HttpPost("orders/{id:guid}/cancel")]
    [RequireModule(ModuleCode.POS, "ORDER_CANCEL")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        await _posService.CancelOrderAsync(id);
        return NoContent();
    }
}