namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryItemTotalView(
    string ItemName,
    double OperationalTotalQuantity,
    double CommodityQuantity,
    double NonCommodityQuantity,
    IReadOnlyList<InventoryItemTotalEntry> Entries)
{
    public bool HasCommodityInventory => CommodityQuantity > 0;

    public bool HasNonCommodityInventory => NonCommodityQuantity > 0;

    public bool HasMixedCommodityStatus => HasCommodityInventory && HasNonCommodityInventory;
}
