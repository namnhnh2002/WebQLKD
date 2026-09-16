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

public class Phase5HospitalityTests
{
    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; }
        public Guid? UserId => Guid.NewGuid();
        public bool IsSuperAdmin => false;
        public bool HasTenant => TenantId != Guid.Empty;
    }

    [Fact]
    public async Task TableService_Should_Open_And_Transfer_Table_Order()
    {
        var tenantId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, new FakeTenantContext(tenantId));
        var area = new TableArea { TenantId = tenantId, Name = "Tầng 1", Code = "F1" };
        var branch = new Branch { TenantId = tenantId, Name = "Chi nhánh chính", Code = "MAIN" };
        var source = new DiningTable { TenantId = tenantId, TableAreaId = area.Id, Name = "Bàn 1", Code = "T1", Status = DiningTableStatus.Available };
        var target = new DiningTable { TenantId = tenantId, TableAreaId = area.Id, Name = "Bàn 2", Code = "T2", Status = DiningTableStatus.Available };
        context.TableAreas.Add(area);
        context.Branches.Add(branch);
        context.Tables.AddRange(source, target);
        await context.SaveChangesAsync();

        var service = new HospitalityService(context, NullLogger<HospitalityService>.Instance);
        var opened = await service.OpenTableAsync(source.Id, "SO-001");
        opened.Status.Should().Be(DiningTableStatus.Occupied);
        await service.TransferTableAsync(source.Id, target.Id);

        (await context.Tables.FindAsync(source.Id))!.Status.Should().Be(DiningTableStatus.Available);
        (await context.Tables.FindAsync(target.Id))!.Status.Should().Be(DiningTableStatus.Occupied);
        (await context.TableOrders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task HospitalityService_Should_Merge_And_Split_Tables()
    {
        var tenantId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, new FakeTenantContext(tenantId));
        var area = new TableArea { TenantId = tenantId, Name = "Tầng 1", Code = "F1" };
        var source = new DiningTable { TenantId = tenantId, TableAreaId = area.Id, Name = "Bàn 1", Code = "T1", Status = DiningTableStatus.Occupied };
        var target = new DiningTable { TenantId = tenantId, TableAreaId = area.Id, Name = "Bàn 2", Code = "T2", Status = DiningTableStatus.Occupied };
        var splitTarget = new DiningTable { TenantId = tenantId, TableAreaId = area.Id, Name = "Bàn 3", Code = "T3", Status = DiningTableStatus.Available };
        context.TableAreas.Add(area);
        context.Tables.AddRange(source, target, splitTarget);
        var sourceOrder = new Order { TenantId = tenantId, BranchId = Guid.NewGuid(), OrderNumber = "SO-1", Status = OrderStatus.Draft };
        var targetOrder = new Order { TenantId = tenantId, BranchId = Guid.NewGuid(), OrderNumber = "SO-2", Status = OrderStatus.Draft };
        var sourceItem = new OrderItem { TenantId = tenantId, Order = sourceOrder, ProductId = Guid.NewGuid(), Quantity = 4, UnitPrice = 10000m, LineTotal = 40000m, Note = "Ít đá" };
        sourceOrder.Items.Add(sourceItem);
        context.Orders.AddRange(sourceOrder, targetOrder);
        context.TableOrders.AddRange(
            new TableOrder { TenantId = tenantId, TableId = source.Id, Order = sourceOrder, ExternalOrderNumber = "SO-1" },
            new TableOrder { TenantId = tenantId, TableId = target.Id, Order = targetOrder, ExternalOrderNumber = "SO-2" });
        await context.SaveChangesAsync();

        var service = new HospitalityService(context, NullLogger<HospitalityService>.Instance);
        await service.MergeTablesAsync(source.Id, target.Id);
        (await context.TableOrders.CountAsync(t => t.TableId == target.Id && t.IsActive)).Should().Be(1);
        (await context.TableOrders.CountAsync(t => t.TableId == source.Id && t.IsActive)).Should().Be(0);
        (await context.Orders.CountAsync(order => order.Status == OrderStatus.Cancelled)).Should().Be(1);
        (await context.Tables.FindAsync(source.Id))!.Status.Should().Be(DiningTableStatus.Available);

        await service.SplitTableAsync(target.Id, splitTarget.Id, new SplitTableRequest(new[] { new TransferOrderItemRequest(sourceItem.Id, 2) }));
        var splitOrder = await context.TableOrders.Where(t => t.TableId == splitTarget.Id && t.IsActive).Select(t => t.Order!).SingleAsync();
        splitOrder.Items.Should().ContainSingle().Which.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task HospitalityService_Should_Create_Kitchen_Order_Topping_And_Combo()
    {
        var tenantId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, new FakeTenantContext(tenantId));
        var category = new Category { TenantId = tenantId, Name = "Đồ uống", Code = "DRINK" };
        var product = new Product { TenantId = tenantId, Category = category, Name = "Cà phê", Code = "CF", SellingPrice = 25000m, Station = ProductStation.Bar };
        var order = new Order { TenantId = tenantId, BranchId = Guid.NewGuid(), OrderNumber = "SO-1", Total = 25000m };
        order.Items.Add(new OrderItem { TenantId = tenantId, Product = product, Quantity = 2, UnitPrice = 25000m, LineTotal = 50000m });
        context.Categories.Add(category);
        context.Products.Add(product);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new HospitalityService(context, NullLogger<HospitalityService>.Instance);
        var kitchenOrder = await service.CreateKitchenOrderAsync(order.Id);
        var topping = await service.CreateToppingAsync(new CreateToppingRequest("TP01", "Trân châu", 5000m));
        var combo = await service.CreateComboAsync(new CreateComboRequest("COMBO01", "Combo cà phê", 28000m, new[] { new ComboItemRequest(product.Id, 1) }));

        kitchenOrder.Items.Should().ContainSingle().Which.Quantity.Should().Be(2);
        topping.Price.Should().Be(5000m);
        combo.Items.Should().ContainSingle().Which.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task HospitalityService_Should_Reject_Duplicate_Open_And_Update_Kitchen_Status()
    {
        var tenantId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, new FakeTenantContext(tenantId));
        var area = new TableArea { TenantId = tenantId, Name = "Tầng 1", Code = "F1" };
        var branch = new Branch { TenantId = tenantId, Name = "Chi nhánh chính", Code = "MAIN" };
        var table = new DiningTable { TenantId = tenantId, TableAreaId = area.Id, Name = "Bàn 1", Code = "T1" };
        context.TableAreas.Add(area);
        context.Branches.Add(branch);
        context.Tables.Add(table);
        await context.SaveChangesAsync();
        var service = new HospitalityService(context, NullLogger<HospitalityService>.Instance);

        await service.OpenTableAsync(table.Id, null);
        var duplicate = () => service.OpenTableAsync(table.Id, null);
        await duplicate.Should().ThrowAsync<InvalidOperationException>();

        var order = new Order { TenantId = tenantId, BranchId = Guid.NewGuid(), OrderNumber = "SO-1" };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        context.KitchenOrders.Add(new KitchenOrder { TenantId = tenantId, OrderId = order.Id });
        await context.SaveChangesAsync();
        var kitchenOrder = await context.KitchenOrders.SingleAsync();
        await service.UpdateKitchenStatusAsync(kitchenOrder.Id, KitchenOrderStatus.Preparing);

        (await context.KitchenOrders.FindAsync(kitchenOrder.Id))!.Status.Should().Be(KitchenOrderStatus.Preparing);
    }
}
