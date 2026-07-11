namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryItemTotalEntry(
    int InventoryEntryId,
    string ItemName,
    string CategoryName,
    string LocationName,
    double CurrentQuantity,
    bool IsCommodity,
    DateTime LastUpdatedAtUtc);
