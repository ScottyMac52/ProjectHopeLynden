namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryEntryListItem(
    int Id,
    string ItemName,
    string LocationName,
    double CurrentQuantity,
    bool IsCommodity,
    DateTime? BestByDate,
    bool IsMenuItem,
    DateTime LastUpdatedAtUtc,
    double IncomingQuantity = 0,
    DateOnly? NextExpectedDate = null);
