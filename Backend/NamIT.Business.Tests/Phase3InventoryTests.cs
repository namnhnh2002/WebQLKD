using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NamIT.Business.Application.DTOs;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Application.Services;
using NamIT.Business.Domain.Entities;
using NamIT.Business.Domain.Enums;
using NamIT.Business.Infrastructure.Persistence;
using Xunit;

namespace NamIT.Business.Tests;

public class Phase3InventoryTests
{
    private class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; }
        public Guid? UserId => Guid.NewGuid();
        public bool IsSuperAdmin => false;
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static ApplicationDbContext CreateContext(Guid tenantId, string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options, new FakeTenantContext(tenantId));
    }

    [Fact]
    public async Task PurchaseOrder_Should_Increase_Inventory_And_Create_Transaction()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();

        await using (var context = CreateContext(tenantId, dbName))
        {
            var category = new Category { TenantId = tenantId, Name = "Nước uống", Code = "DRINK" };
            var supplier = new Supplier { TenantId = tenantId, Name = "Nhà cung cấp A", Code = "SUPA" };
            var branch = new Branch { TenantId = tenantId, Name = "Chi nhánh chính", Code = "MAIN" };
            context.Categories.Add(category);
            context.Suppliers.Add(supplier);
            context.Branches.Add(branch);
            await context.SaveChangesAsync();

            var product = new Product { TenantId = tenantId, CategoryId = category.Id, Name = "Cà phê", Code = "CF", Unit = "ly", CostPrice = 15000m, SellingPrice = 25000m, MinStock = 5 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new InventoryService(context, NullLogger<InventoryService>.Instance);
            var result = await service.CreatePurchaseAsync(new CreatePurchaseRequest(branch.Id, supplier.Id, "PO-001", DateTime.UtcNow, new[]
            {
                new PurchaseItemRequest(product.Id, 10, 15000m)
            }));

            result.TotalQuantity.Should().Be(10);
            var transactionCount = await context.InventoryTransactions.CountAsync();
            transactionCount.Should().Be(1);
            var stock = await service.GetCurrentStockAsync(product.Id);
            stock.Should().Be(10);
        }
    }

    [Fact]
    public async Task InventoryService_Should_Return_Low_Stock_Products()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();

        await using (var context = CreateContext(tenantId, dbName))
        {
            var category = new Category { TenantId = tenantId, Name = "Nước uống", Code = "DRINK" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var product = new Product { TenantId = tenantId, CategoryId = category.Id, Name = "Nước cam", Code = "NC", Unit = "chai", CostPrice = 12000m, SellingPrice = 20000m, MinStock = 5 };
            context.Products.Add(product);
            context.InventoryTransactions.Add(new InventoryTransaction { TenantId = tenantId, BranchId = Guid.NewGuid(), ProductId = product.Id, Quantity = 2, Type = InventoryTransactionType.PURCHASE, Note = "Nhập ban đầu" });
            await context.SaveChangesAsync();

            var service = new InventoryService(context, NullLogger<InventoryService>.Instance);
            var lowStock = await service.GetLowStockAsync();

            lowStock.Should().ContainSingle();
            lowStock.First().ProductId.Should().Be(product.Id);
        }
    }
}
