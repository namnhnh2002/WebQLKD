using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Domain.Entities;
using NamIT.Business.Domain.Enums;

namespace NamIT.Business.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IApplicationDbContext db, ILogger<CategoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var items = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return items.Select(c => new CategoryDto(c.Id, c.TenantId, c.Code, c.Name, c.Description, c.ParentCategoryId, c.IsDeleted)).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(Guid id)
    {
        var item = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy danh mục.");

        return new CategoryDto(item.Id, item.TenantId, item.Code, item.Name, item.Description, item.ParentCategoryId, item.IsDeleted);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên danh mục không được để trống.");

        var item = new Category
        {
            Code = string.IsNullOrWhiteSpace(request.Code) ? Guid.NewGuid().ToString()[..8].ToUpperInvariant() : request.Code,
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId
        };

        _db.Categories.Add(item);
        await _db.SaveChangesAsync();

        return new CategoryDto(item.Id, item.TenantId, item.Code, item.Name, item.Description, item.ParentCategoryId, item.IsDeleted);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var item = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy danh mục.");

        if (!string.IsNullOrWhiteSpace(request.Code)) item.Code = request.Code;
        if (!string.IsNullOrWhiteSpace(request.Name)) item.Name = request.Name;
        if (request.Description != null) item.Description = request.Description;
        if (request.ParentCategoryId.HasValue) item.ParentCategoryId = request.ParentCategoryId;

        await _db.SaveChangesAsync();

        return new CategoryDto(item.Id, item.TenantId, item.Code, item.Name, item.Description, item.ParentCategoryId, item.IsDeleted);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy danh mục.");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IApplicationDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var items = await _db.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();
        var productIds = items.Select(product => product.Id).ToList();
        var stock = await _db.InventoryTransactions
            .Where(transaction => productIds.Contains(transaction.ProductId))
            .GroupBy(transaction => transaction.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(transaction => transaction.Type == InventoryTransactionType.SALE || transaction.Type == InventoryTransactionType.DAMAGE ? -transaction.Quantity : transaction.Quantity) })
            .ToDictionaryAsync(item => item.ProductId, item => item.Quantity);

        return items.Select(p => new ProductDto(
            p.Id,
            p.TenantId,
            p.CategoryId,
            p.Category?.Name ?? string.Empty,
            p.Code,
            p.Barcode,
            p.Name,
            p.Description,
            p.Unit,
            p.CostPrice,
            p.SellingPrice,
            p.MinStock,
            p.ImageUrl,
            (int)p.Status,
            p.IsDeleted,
            stock.GetValueOrDefault(p.Id))).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

        var currentStock = await _db.InventoryTransactions
            .Where(transaction => transaction.ProductId == product.Id)
            .SumAsync(transaction => transaction.Type == InventoryTransactionType.SALE || transaction.Type == InventoryTransactionType.DAMAGE ? -transaction.Quantity : transaction.Quantity);
        return new ProductDto(product.Id, product.TenantId, product.CategoryId, product.Category?.Name ?? string.Empty, product.Code, product.Barcode, product.Name, product.Description, product.Unit, product.CostPrice, product.SellingPrice, product.MinStock, product.ImageUrl, (int)product.Status, product.IsDeleted, currentStock);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên sản phẩm không được để trống.");

        if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            throw new InvalidOperationException("Danh mục sản phẩm không hợp lệ.");

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Code = string.IsNullOrWhiteSpace(request.Code) ? Guid.NewGuid().ToString()[..8].ToUpperInvariant() : request.Code,
            Barcode = request.Barcode,
            Name = request.Name,
            Description = request.Description,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "pcs" : request.Unit,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            MinStock = request.MinStock,
            ImageUrl = request.ImageUrl,
            Status = (EntityStatus)request.Status
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

        if (request.CategoryId.HasValue) product.CategoryId = request.CategoryId.Value;
        if (!string.IsNullOrWhiteSpace(request.Code)) product.Code = request.Code;
        if (request.Barcode != null) product.Barcode = request.Barcode;
        if (!string.IsNullOrWhiteSpace(request.Name)) product.Name = request.Name;
        if (request.Description != null) product.Description = request.Description;
        if (!string.IsNullOrWhiteSpace(request.Unit)) product.Unit = request.Unit;
        if (request.CostPrice.HasValue) product.CostPrice = request.CostPrice.Value;
        if (request.SellingPrice.HasValue) product.SellingPrice = request.SellingPrice.Value;
        if (request.MinStock.HasValue) product.MinStock = request.MinStock.Value;
        if (request.ImageUrl != null) product.ImageUrl = request.ImageUrl;
        if (request.Status.HasValue) product.Status = (EntityStatus)request.Status.Value;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(product.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IApplicationDbContext db, ILogger<CustomerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        var list = await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return list.Select(c => new CustomerDto(c.Id, c.TenantId, c.Code, c.Name, c.Phone, c.Email, c.Address, c.GroupId, c.CreditLimit, (int)c.Status, c.IsDeleted)).ToList();
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách hàng.");

        return new CustomerDto(entity.Id, entity.TenantId, entity.Code, entity.Name, entity.Phone, entity.Email, entity.Address, entity.GroupId, entity.CreditLimit, (int)entity.Status, entity.IsDeleted);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên khách hàng không được để trống.");

        var entity = new Customer
        {
            Code = string.IsNullOrWhiteSpace(request.Code) ? "KH" + Guid.NewGuid().ToString()[..8].ToUpperInvariant() : request.Code,
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            GroupId = request.GroupId,
            CreditLimit = request.CreditLimit,
            Status = request.Status.HasValue ? (EntityStatus)request.Status.Value : EntityStatus.Active
        };

        _db.Customers.Add(entity);
        await _db.SaveChangesAsync();

        return new CustomerDto(entity.Id, entity.TenantId, entity.Code, entity.Name, entity.Phone, entity.Email, entity.Address, entity.GroupId, entity.CreditLimit, (int)entity.Status, entity.IsDeleted);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách hàng.");

        if (!string.IsNullOrWhiteSpace(request.Code)) entity.Code = request.Code;
        if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name;
        if (request.Phone != null) entity.Phone = request.Phone;
        if (request.Email != null) entity.Email = request.Email;
        if (request.Address != null) entity.Address = request.Address;
        if (request.GroupId != null) entity.GroupId = request.GroupId;
        if (request.CreditLimit.HasValue) entity.CreditLimit = request.CreditLimit.Value;
        if (request.Status.HasValue) entity.Status = (EntityStatus)request.Status.Value;

        await _db.SaveChangesAsync();
        return new CustomerDto(entity.Id, entity.TenantId, entity.Code, entity.Name, entity.Phone, entity.Email, entity.Address, entity.GroupId, entity.CreditLimit, (int)entity.Status, entity.IsDeleted);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách hàng.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

public class SupplierService : ISupplierService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(IApplicationDbContext db, ILogger<SupplierService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        var list = await _db.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        return list.Select(s => new SupplierDto(s.Id, s.TenantId, s.Code, s.Name, s.Phone, s.Email, s.Address, s.TaxCode, (int)s.Status, s.IsDeleted)).ToList();
    }

    public async Task<SupplierDto> GetByIdAsync(Guid id)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy nhà cung cấp.");

        return new SupplierDto(entity.Id, entity.TenantId, entity.Code, entity.Name, entity.Phone, entity.Email, entity.Address, entity.TaxCode, (int)entity.Status, entity.IsDeleted);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Tên nhà cung cấp không được để trống.");

        var entity = new Supplier
        {
            Code = string.IsNullOrWhiteSpace(request.Code) ? "NCC" + Guid.NewGuid().ToString()[..8].ToUpperInvariant() : request.Code,
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            TaxCode = request.TaxCode,
            Status = request.Status.HasValue ? (EntityStatus)request.Status.Value : EntityStatus.Active
        };

        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync();

        return new SupplierDto(entity.Id, entity.TenantId, entity.Code, entity.Name, entity.Phone, entity.Email, entity.Address, entity.TaxCode, (int)entity.Status, entity.IsDeleted);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy nhà cung cấp.");

        if (!string.IsNullOrWhiteSpace(request.Code)) entity.Code = request.Code;
        if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name;
        if (request.Phone != null) entity.Phone = request.Phone;
        if (request.Email != null) entity.Email = request.Email;
        if (request.Address != null) entity.Address = request.Address;
        if (request.TaxCode != null) entity.TaxCode = request.TaxCode;
        if (request.Status.HasValue) entity.Status = (EntityStatus)request.Status.Value;

        await _db.SaveChangesAsync();
        return new SupplierDto(entity.Id, entity.TenantId, entity.Code, entity.Name, entity.Phone, entity.Email, entity.Address, entity.TaxCode, (int)entity.Status, entity.IsDeleted);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy nhà cung cấp.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
