using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NamIT.Business.Application.Interfaces;
using NamIT.Business.Domain.Entities;
using NamIT.Business.Domain.Enums;
using NamIT.Business.Infrastructure.Persistence;
using Xunit;

namespace NamIT.Business.Tests;

/// <summary>
/// Test bắt buộc theo yêu cầu: Tenant A KHÔNG được truy cập dữ liệu của Tenant B,
/// dù có cố tình gửi TenantId khác trong request (ở đây mô phỏng bằng cách chỉ
/// thay đổi ITenantContext trả về, vì đó là nguồn duy nhất backend tin tưởng).
/// </summary>
public class TenantIsolationTests
{
    private class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid TenantId { get; }
        public Guid? UserId => null;
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
    public async Task User_Of_TenantA_Cannot_See_Products_Of_TenantB()
    {
        var sharedDbName = Guid.NewGuid().ToString();
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        // Seed dữ liệu: dùng context "God mode" bằng cách ghi thẳng TenantId (không qua SaveChangesAsync tự gán)
        using (var seedContext = CreateContext(tenantAId, sharedDbName))
        {
            seedContext.Branches.Add(new Branch { TenantId = tenantAId, Name = "Chi nhánh A", Code = "A1" });
            await seedContext.SaveChangesAsync();
        }
        using (var seedContextB = CreateContext(tenantBId, sharedDbName))
        {
            seedContextB.Branches.Add(new Branch { TenantId = tenantBId, Name = "Chi nhánh B", Code = "B1" });
            await seedContextB.SaveChangesAsync();
        }

        // Act: đăng nhập với vai trò Tenant A -> query Branches
        using var contextAsTenantA = CreateContext(tenantAId, sharedDbName);
        var branchesVisibleToTenantA = await contextAsTenantA.Branches.ToListAsync();

        // Assert
        branchesVisibleToTenantA.Should().HaveCount(1);
        branchesVisibleToTenantA.Single().TenantId.Should().Be(tenantAId);
        branchesVisibleToTenantA.Should().NotContain(b => b.TenantId == tenantBId);
    }

    [Fact]
    public async Task New_Entity_Always_Gets_TenantId_From_ServerSide_Context_Not_From_Caller()
    {
        var sharedDbName = Guid.NewGuid().ToString();
        var tenantAId = Guid.NewGuid();

        using var context = CreateContext(tenantAId, sharedDbName);

        // Cố tình để TenantId = Guid.Empty như thể client không gửi / cố gửi tenant khác -
        // SaveChangesAsync phải tự gán theo server-side context, không tin client.
        var branch = new Branch { Name = "Chi nhánh test", Code = "T1" };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();

        branch.TenantId.Should().Be(tenantAId);
    }

    [Fact]
    public async Task Soft_Deleted_Branch_Is_Excluded_By_Default_Query()
    {
        var sharedDbName = Guid.NewGuid().ToString();
        var tenantAId = Guid.NewGuid();

        using (var context = CreateContext(tenantAId, sharedDbName))
        {
            context.Branches.Add(new Branch { TenantId = tenantAId, Name = "Sẽ xóa", Code = "DEL1", IsDeleted = true });
            context.Branches.Add(new Branch { TenantId = tenantAId, Name = "Còn hoạt động", Code = "OK1", IsDeleted = false });
            await context.SaveChangesAsync();
        }

        using var readContext = CreateContext(tenantAId, sharedDbName);
        var branches = await readContext.Branches.ToListAsync();

        branches.Should().ContainSingle();
        branches.Single().Code.Should().Be("OK1");
    }
}
