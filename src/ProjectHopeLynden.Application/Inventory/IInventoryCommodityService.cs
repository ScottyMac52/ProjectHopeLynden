namespace ProjectHopeLynden.Application.Inventory;

public interface IInventoryCommodityService
{
    Task<IReadOnlyList<CommodityInventoryEntryListItem>> GetCommodityInventoryAsync(
        CancellationToken cancellationToken = default);

    Task<InventoryItemTotalView?> GetItemTotalAsync(
        string itemName,
        CancellationToken cancellationToken = default);
}
