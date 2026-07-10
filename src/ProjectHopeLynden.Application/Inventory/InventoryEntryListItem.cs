namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryEntryListItem(
    int Id,
    string ItemName,
    string LocationName,
    int CurrentQuantity,
    bool IsCommodity,
    DateTime? BestByDate,
    bool IsMenuItem,
    DateTime LastUpdatedAtUtc);
