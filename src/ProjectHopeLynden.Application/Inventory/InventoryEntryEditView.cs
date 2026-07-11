namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryEntryEditView(
    int InventoryEntryId,
    string ItemName,
    int CategoryId,
    int LocationId,
    double CurrentQuantity,
    DateTime? BestByDate,
    bool IsCommodity,
    bool IsMenuItem);
