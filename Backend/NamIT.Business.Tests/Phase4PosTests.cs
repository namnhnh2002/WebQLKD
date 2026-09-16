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

public class Phase4PosTests
{
    private sealed class FakeTenantContext : ITenantContext
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
    public async Task PosOrder_Should_Create_Payment_Debt_And_Sale_Transaction()
    {
        var tenantId = Guid.NewGuid();
        await using var context = CreateContext(tenantId, Guid.NewGuid().ToString());
        var category = new Category { TenantId = tenantId, Code = "DRINK", Name = "Đồ uống" };
        var branch = new Branch { TenantId = tenantId, Code = "MAIN", Name = "Chi nhánh chính" };
        var customer = new Customer { TenantId = tenantId, Code = "CUS01", Name = "Khách hàng công nợ" };
        context.Categories.Add(category);
        context.Branches.Add(branch);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var product = new Product
        {
            TenantId = tenantId,
            CategoryId = category.Id,
            Code = "CF01",
            Name = "Cà phê",
            Unit = "ly",
            CostPrice = 10000m,
            SellingPrice = 25000m,
            MinStock = 2
        };
        context.Products.Add(product);
        context.InventoryTransactions.Add(new InventoryTransaction
        {
            TenantId = tenantId,
            BranchId = branch.Id,
            ProductId = product.Id,
            Type = InventoryTransactionType.PURCHASE,
            Quantity = 10
        });
        await context.SaveChangesAsync();

        var service = new PosService(context, NullLogger<PosService>.Instance);
        var result = await service.CreateOrderAsync(new CreateOrderRequest(
            branch.Id,
            customer.Id,
            new[] { new CartItemRequest(product.Id, 2, 25000m) },
            0m,
            new[] { new CreatePaymentRequest(30000m, PaymentMethod.CASH, "") }));

        result.Total.Should().Be(50000m);
        result.PaidAmount.Should().Be(30000m);
        result.DebtAmount.Should().Be(20000m);
        (await context.Orders.CountAsync()).Should().Be(1);
        (await context.Payments.CountAsync()).Should().Be(1);
        (await context.Debts.CountAsync()).Should().Be(1);
        (await context.DebtTransactions.CountAsync()).Should().Be(1);
        (await context.InventoryTransactions.CountAsync(t => t.Type == InventoryTransactionType.SALE)).Should().Be(1);
    }

    [Fact]
    public async Task PosOrder_Should_Reject_Debt_For_Guest()
    {
        var tenantId = Guid.NewGuid();
        await using var context = CreateContext(tenantId, Guid.NewGuid().ToString());
        var category = new Category { TenantId = tenantId, Code = "DRINK", Name = "Đồ uống" };
        var branch = new Branch { TenantId = tenantId, Code = "MAIN", Name = "Chi nhánh chính" };
        var product = new Product { TenantId = tenantId, Category = category, Code = "CF01", Name = "Cà phê", SellingPrice = 25000m };
        context.Branches.Add(branch);
        context.Products.Add(product);
        context.InventoryTransactions.Add(new InventoryTransaction { TenantId = tenantId, BranchId = branch.Id, ProductId = product.Id, Type = InventoryTransactionType.PURCHASE, Quantity = 5 });
        await context.SaveChangesAsync();

        var service = new PosService(context, NullLogger<PosService>.Instance);
        var action = () => service.CreateOrderAsync(new CreateOrderRequest(branch.Id, null, new[] { new CartItemRequest(product.Id, 1, 1m) }, 0m, Array.Empty<CreatePaymentRequest>()));

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*chọn khách hàng*");
        (await context.Orders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Cancelling_Order_Should_Return_Inventory_And_Keep_Order_History()
    {
        var tenantId = Guid.NewGuid();
        await using var context = CreateContext(tenantId, Guid.NewGuid().ToString());
        var category = new Category { TenantId = tenantId, Code = "DRINK", Name = "Đồ uống" };
        var branch = new Branch { TenantId = tenantId, Code = "MAIN", Name = "Chi nhánh chính" };
        var product = new Product { TenantId = tenantId, Category = category, Code = "CF01", Name = "Cà phê", SellingPrice = 25000m };
        context.Branches.Add(branch);
        context.Products.Add(product);
        context.InventoryTransactions.Add(new InventoryTransaction { TenantId = tenantId, BranchId = branch.Id, ProductId = product.Id, Type = InventoryTransactionType.PURCHASE, Quantity = 5 });
        await context.SaveChangesAsync();
        var service = new PosService(context, NullLogger<PosService>.Instance);
        var order = await service.CreateOrderAsync(new CreateOrderRequest(branch.Id, null, new[] { new CartItemRequest(product.Id, 1, 1m) }, 0m, new[] { new CreatePaymentRequest(25000m, PaymentMethod.CASH, null) }));

        await service.CancelOrderAsync(order.Id);

        (await context.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Cancelled);
        (await context.InventoryTransactions.CountAsync(transaction => transaction.Type == InventoryTransactionType.RETURN)).Should().Be(1);
    }

    [Fact]
    public async Task PosOrder_Should_Reject_When_Stock_Is_Insufficient()
    {
        var tenantId = Guid.NewGuid();
        await using var context = CreateContext(tenantId, Guid.NewGuid().ToString());
        var category = new Category { TenantId = tenantId, Code = "DRINK", Name = "Đồ uống" };
        var branch = new Branch { TenantId = tenantId, Code = "MAIN", Name = "Chi nhánh chính" };
        var product = new Product { TenantId = tenantId, Category = category, Code = "CF01", Name = "Cà phê", SellingPrice = 25000m };
        context.Branches.Add(branch);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var service = new PosService(context, NullLogger<PosService>.Instance);
        var action = () => service.CreateOrderAsync(new CreateOrderRequest(
            branch.Id,
            null,
            new[] { new CartItemRequest(product.Id, 1, 25000m) },
            0m,
            Array.Empty<CreatePaymentRequest>()));

        await action.Should().ThrowAsync<InvalidOperationException>();
        (await context.Orders.CountAsync()).Should().Be(0);
    }
}
