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

public class Phase2CoreBusinessTests
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
    public async Task ProductService_Should_Only_Return_Tenant_Products()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        await using (var seed = CreateContext(tenantAId, dbName))
        {
            var categoryA = new Category { TenantId = tenantAId, Name = "Đồ uống", Code = "DRINK" };
            seed.Categories.Add(categoryA);
            await seed.SaveChangesAsync();

            seed.Products.Add(new Product { TenantId = tenantAId, CategoryId = categoryA.Id, Name = "Cà phê sữa", Code = "CF001", Barcode = "CF001", Unit = "ly", SellingPrice = 25000m, CostPrice = 15000m, MinStock = 10, Status = EntityStatus.Active });
            await seed.SaveChangesAsync();
        }

        await using (var seedB = CreateContext(tenantBId, dbName))
        {
            var categoryB = new Category { TenantId = tenantBId, Name = "Thời trang", Code = "FASHION" };
            seedB.Categories.Add(categoryB);
            await seedB.SaveChangesAsync();

            seedB.Products.Add(new Product { TenantId = tenantBId, CategoryId = categoryB.Id, Name = "Áo thun", Code = "SHIRT01", Barcode = "SHIRT01", Unit = "chiếc", SellingPrice = 150000m, CostPrice = 90000m, MinStock = 5, Status = EntityStatus.Active });
            await seedB.SaveChangesAsync();
        }

        await using var tenantAContext = CreateContext(tenantAId, dbName);
        var productService = new ProductService(tenantAContext, new NullLogger<ProductService>());
        var result = await productService.GetAllAsync();

        result.Should().HaveCount(1);
        result.Single().TenantId.Should().Be(tenantAId);
    }

    [Fact]
    public async Task CustomerService_Should_Create_And_Query_By_Tenant()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantAId = Guid.NewGuid();

        await using var context = CreateContext(tenantAId, dbName);
        var service = new CustomerService(context, new NullLogger<CustomerService>());

        var created = await service.CreateAsync(new CreateCustomerRequest("KH0001", "Nguyễn Văn A", "0900000001", "a@test.com", "HCM", "GROUP1", 500000m, (int)EntityStatus.Active));

        created.TenantId.Should().Be(tenantAId);
        created.Name.Should().Be("Nguyễn Văn A");

        var list = await service.GetAllAsync();
        list.Should().HaveCount(1);
    }
}
