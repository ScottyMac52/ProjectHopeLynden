namespace ProjectHopeLynden.Application.Inventory;

public sealed record InventoryEntryHistoryView(
    int InventoryEntryId,
    int CategoryId,
    string ItemName,
    string CategoryName,
    string LocationName,
    bool IsCommodity,
    double CurrentQuantity,
    IReadOnlyList<InventoryCountHistoryListItem> Records)
{
    public bool HasHistory => Records.Count > 0;
}
