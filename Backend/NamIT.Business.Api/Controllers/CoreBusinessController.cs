using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Api.Authorization;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class CoreBusinessController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly ICustomerService _customerService;
    private readonly ISupplierService _supplierService;
    private readonly IApplicationDbContext _db;

    public CoreBusinessController(
        ICategoryService categoryService,
        IProductService productService,
        ICustomerService customerService,
        ISupplierService supplierService,
        IApplicationDbContext db)
    {
        _categoryService = categoryService;
        _productService = productService;
        _customerService = customerService;
        _supplierService = supplierService;
        _db = db;
    }

    [HttpGet("categories")]
    [RequireModule(ModuleCode.PRODUCT, "PRODUCT_VIEW")]
    public async Task<IActionResult> GetCategories() => Ok(ApiResponse<List<CategoryDto>>.Ok(await _categoryService.GetAllAsync()));

    [HttpPost("categories")]
    [RequireModule(ModuleCode.PRODUCT, "PRODUCT_CREATE")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var item = await _categoryService.CreateAsync(request);
        return Ok(ApiResponse<CategoryDto>.Ok(item, "Tạo danh mục thành công."));
    }

    [HttpGet("products")]
    [RequireModule(ModuleCode.PRODUCT, "PRODUCT_VIEW")]
    public async Task<IActionResult> GetProducts() => Ok(ApiResponse<List<ProductDto>>.Ok(await _productService.GetAllAsync()));

    [HttpPost("products")]
    [RequireModule(ModuleCode.PRODUCT, "PRODUCT_CREATE")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var item = await _productService.CreateAsync(request);
        return Ok(ApiResponse<ProductDto>.Ok(item, "Tạo sản phẩm thành công."));
    }

    [HttpGet("customers")]
    [RequireModule(ModuleCode.CUSTOMER, "CUSTOMER_VIEW")]
    public async Task<IActionResult> GetCustomers() => Ok(ApiResponse<List<CustomerDto>>.Ok(await _customerService.GetAllAsync()));

    [HttpPost("customers")]
    [RequireModule(ModuleCode.CUSTOMER, "CUSTOMER_CREATE")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var item = await _customerService.CreateAsync(request);
        return Ok(ApiResponse<CustomerDto>.Ok(item, "Tạo khách hàng thành công."));
    }

    [HttpGet("suppliers")]
    [RequireModule(ModuleCode.SUPPLIER, "SUPPLIER_VIEW")]
    public async Task<IActionResult> GetSuppliers() => Ok(ApiResponse<List<SupplierDto>>.Ok(await _supplierService.GetAllAsync()));

    [HttpPost("suppliers")]
    [RequireModule(ModuleCode.SUPPLIER, "SUPPLIER_CREATE")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        var item = await _supplierService.CreateAsync(request);
        return Ok(ApiResponse<SupplierDto>.Ok(item, "Tạo nhà cung cấp thành công."));
    }

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches()
    {
        var branches = await _db.Branches.AsNoTracking().OrderBy(branch => branch.Name).Select(branch => new
        {
            branch.Id,
            branch.Name,
            branch.Code
        }).ToListAsync();
        return Ok(ApiResponse<object>.Ok(branches));
    }
}
