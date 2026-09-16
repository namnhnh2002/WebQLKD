namespace NamIT.Business.Application.DTOs;

public record CategoryDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    bool IsDeleted);

public record CreateCategoryRequest(
    string Code,
    string Name,
    string? Description,
    Guid? ParentCategoryId);

public record UpdateCategoryRequest(
    string? Code,
    string? Name,
    string? Description,
    Guid? ParentCategoryId);

public record ProductDto(
    Guid Id,
    Guid TenantId,
    Guid CategoryId,
    string CategoryName,
    string Code,
    string? Barcode,
    string Name,
    string? Description,
    string Unit,
    decimal CostPrice,
    decimal SellingPrice,
    int MinStock,
    string? ImageUrl,
    int Status,
    bool IsDeleted,
    int CurrentStock = 0);

public record CreateProductRequest(
    Guid CategoryId,
    string Code,
    string? Barcode,
    string Name,
    string? Description,
    string Unit,
    decimal CostPrice,
    decimal SellingPrice,
    int MinStock,
    string? ImageUrl,
    int Status);

public record UpdateProductRequest(
    Guid? CategoryId,
    string? Code,
    string? Barcode,
    string? Name,
    string? Description,
    string? Unit,
    decimal? CostPrice,
    decimal? SellingPrice,
    int? MinStock,
    string? ImageUrl,
    int? Status);

public record CustomerDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? GroupId,
    decimal CreditLimit,
    int Status,
    bool IsDeleted);

public record CreateCustomerRequest(
    string Code,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? GroupId,
    decimal CreditLimit,
    int? Status);

public record UpdateCustomerRequest(
    string? Code,
    string? Name,
    string? Phone,
    string? Email,
    string? Address,
    string? GroupId,
    decimal? CreditLimit,
    int? Status);

public record SupplierDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxCode,
    int Status,
    bool IsDeleted);

public record CreateSupplierRequest(
    string Code,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxCode,
    int? Status);

public record UpdateSupplierRequest(
    string? Code,
    string? Name,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxCode,
    int? Status);
