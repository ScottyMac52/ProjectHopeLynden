namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventorySearchRow(
    int InventoryEntryId,
    string ItemName,
    string CategoryName,
    string LocationName,
    double CurrentQuantity,
    bool IsCommodity,
    DateTime? BestByDate,
    bool IsMenuItem,
    DateTime LastUpdatedAtUtc);
