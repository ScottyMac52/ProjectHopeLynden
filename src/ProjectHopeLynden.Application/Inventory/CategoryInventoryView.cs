namespace ProjectHopeLynden.Application.Inventory;

public sealed record CategoryInventoryView(
    int CategoryId,
    string CategoryName,
    IReadOnlyList<InventoryEntryListItem> Entries)
{
    public bool HasEntries => Entries.Count > 0;
}
