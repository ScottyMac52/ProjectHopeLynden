namespace ProjectHopeLynden.Application.Inventory;

public sealed record CommodityInventoryEntryListItem(
    int InventoryEntryId,
    string ItemName,
    string CategoryName,
    string LocationName,
    int CurrentQuantity,
    DateTime? BestByDate,
    bool IsMenuItem,
    DateTime LastUpdatedAtUtc);
