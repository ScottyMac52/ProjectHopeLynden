namespace ProjectHopeLynden.Application.Inventory;

public sealed record CommodityInventoryEntryListItem(
    int InventoryEntryId,
    string ItemName,
    string CategoryName,
    string LocationName,
    double CurrentQuantity,
    DateTime? BestByDate,
    bool IsMenuItem,
    DateTime LastUpdatedAtUtc);
