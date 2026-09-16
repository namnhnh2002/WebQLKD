using FluentAssertions;
using NamIT.Business.Domain.Enums;
using Xunit;

namespace NamIT.Business.Tests;

public class Phase1AModuleCatalogTests
{
    [Fact]
    public void Cafe_Should_Enable_Hospitality_Modules()
    {
        var modules = ModuleCatalog.For(BusinessTypeCode.CAFE);

        modules.Should().Contain([ModuleCode.POS, ModuleCode.PRODUCT, ModuleCode.TABLE, ModuleCode.KITCHEN, ModuleCode.TOPPING, ModuleCode.COMBO]);
        modules.Should().NotContain(ModuleCode.BILLIARD);
    }

    [Fact]
    public void Billiard_Should_Enable_Time_Pricing_Without_Hospitality_Table()
    {
        var modules = ModuleCatalog.For(BusinessTypeCode.BILLIARD);

        modules.Should().Contain([ModuleCode.POS, ModuleCode.BILLIARD, ModuleCode.TIME_PRICING]);
        modules.Should().NotContain(ModuleCode.TABLE);
    }

    [Fact]
    public void Fashion_Should_Enable_Variant_Attributes()
    {
        var modules = ModuleCatalog.For(BusinessTypeCode.FASHION);

        modules.Should().Contain([ModuleCode.PRODUCT_VARIANT, ModuleCode.SIZE, ModuleCode.COLOR, ModuleCode.BARCODE]);
        modules.Should().NotContain(ModuleCode.KITCHEN);
    }
}
