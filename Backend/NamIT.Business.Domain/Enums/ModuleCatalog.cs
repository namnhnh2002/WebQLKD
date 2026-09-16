namespace NamIT.Business.Domain.Enums;

public static class ModuleCatalog
{
    public static IReadOnlyCollection<ModuleCode> CoreModules { get; } =
    [
        ModuleCode.POS,
        ModuleCode.PRODUCT,
        ModuleCode.INVENTORY,
        ModuleCode.CUSTOMER,
        ModuleCode.PAYMENT,
        ModuleCode.DEBT,
        ModuleCode.REPORT
    ];

    public static IReadOnlyCollection<ModuleCode> For(BusinessTypeCode businessType) => businessType switch
    {
        BusinessTypeCode.CAFE or BusinessTypeCode.RESTAURANT or BusinessTypeCode.PUB =>
            [..CoreModules, ModuleCode.SUPPLIER, ModuleCode.TABLE, ModuleCode.KITCHEN, ModuleCode.TOPPING, ModuleCode.COMBO],
        BusinessTypeCode.MILK_TEA =>
            [..CoreModules, ModuleCode.SUPPLIER, ModuleCode.TOPPING, ModuleCode.COMBO],
        BusinessTypeCode.BILLIARD =>
            [..CoreModules, ModuleCode.BILLIARD, ModuleCode.TIME_PRICING],
        BusinessTypeCode.FASHION =>
            [..CoreModules, ModuleCode.SUPPLIER, ModuleCode.PRODUCT_VARIANT, ModuleCode.SIZE, ModuleCode.COLOR, ModuleCode.BARCODE],
        BusinessTypeCode.RETAIL or BusinessTypeCode.GROCERY =>
            [..CoreModules, ModuleCode.SUPPLIER, ModuleCode.BARCODE],
        BusinessTypeCode.SALON =>
            [..CoreModules, ModuleCode.APPOINTMENT],
        _ => CoreModules
    };
}
