namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryItemTotalEntry(
    int InventoryEntryId,
    string ItemName,
    string CategoryName,
    string LocationName,
    int CurrentQuantity,
    bool IsCommodity,
    DateTime LastUpdatedAtUtc);
